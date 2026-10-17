using System;
using System.Collections.Generic;

namespace CommitDock.Models
{
    /// <summary>
    ///     Post-processes the lines of a parsed text diff:
    ///     1. marks blocks of removed lines that were added back elsewhere in the file as moved, and
    ///     2. pairs the remaining removed/added lines of each change block by similarity and computes
    ///        word-level highlights for every pair (so blocks with a different number of removed and
    ///        added lines get highlights too).
    /// </summary>
    public static class TextDiffRefiner
    {
        public static void Refine(List<TextDiffLine> lines)
        {
            DetectMovedLines(lines);

            var deleted = new List<TextDiffLine>();
            var added = new List<TextDiffLine>();
            foreach (var line in lines)
            {
                if (line.Type == TextDiffLineType.Deleted)
                {
                    if (added.Count > 0)
                        HighlightBlock(deleted, added);
                    deleted.Add(line);
                }
                else if (line.Type == TextDiffLineType.Added)
                {
                    added.Add(line);
                }
                else
                {
                    HighlightBlock(deleted, added);
                }
            }

            HighlightBlock(deleted, added);
        }

        #region Moved lines
        private static void DetectMovedLines(List<TextDiffLine> lines)
        {
            var count = lines.Count;
            var changed = 0;
            foreach (var line in lines)
            {
                if (line.Type == TextDiffLineType.Added || line.Type == TextDiffLineType.Deleted)
                    changed++;
            }
            if (changed > MAX_MOVE_DETECTION_LINES)
                return;

            var normalized = new string[count];
            var addedByContent = new Dictionary<string, List<int>>(StringComparer.Ordinal);

            for (var i = 0; i < count; i++)
            {
                var line = lines[i];
                if (line.Type != TextDiffLineType.Added && line.Type != TextDiffLineType.Deleted)
                    continue;

                normalized[i] = line.Content.Trim();
                if (line.Type == TextDiffLineType.Added && normalized[i].Length > 0)
                {
                    if (!addedByContent.TryGetValue(normalized[i], out var list))
                    {
                        list = new List<int>();
                        addedByContent.Add(normalized[i], list);
                    }
                    list.Add(i);
                }
            }

            if (addedByContent.Count == 0)
                return;

            var usedAdded = new bool[count];
            var start = 0;
            while (start < count)
            {
                if (lines[start].Type != TextDiffLineType.Deleted ||
                    normalized[start].Length == 0 ||
                    !addedByContent.TryGetValue(normalized[start], out var candidates))
                {
                    start++;
                    continue;
                }

                var bestAdded = -1;
                var bestLength = 0;
                var checkedCandidates = 0;
                foreach (var candidate in candidates)
                {
                    if (usedAdded[candidate] || IsSameChangeBlock(lines, start, candidate))
                        continue;

                    if (++checkedCandidates > MAX_MOVE_CANDIDATES)
                        break;

                    var length = 0;
                    while (start + length < count &&
                           candidate + length < count &&
                           lines[start + length].Type == TextDiffLineType.Deleted &&
                           lines[candidate + length].Type == TextDiffLineType.Added &&
                           !usedAdded[candidate + length] &&
                           normalized[start + length] == normalized[candidate + length])
                        length++;

                    if (length > bestLength)
                    {
                        bestLength = length;
                        bestAdded = candidate;
                    }
                }

                if (bestAdded < 0 || !IsSignificantBlock(normalized, start, bestLength))
                {
                    start++;
                    continue;
                }

                for (var k = 0; k < bestLength; k++)
                {
                    lines[start + k].IsMoved = true;
                    lines[bestAdded + k].IsMoved = true;
                    usedAdded[bestAdded + k] = true;
                }

                start += bestLength;
            }
        }

        // A removed block re-added inside the same contiguous change (for example a re-indented line)
        // is an edit in place, not a move.
        private static bool IsSameChangeBlock(List<TextDiffLine> lines, int a, int b)
        {
            var from = Math.Min(a, b);
            var to = Math.Max(a, b);
            for (var i = from + 1; i < to; i++)
            {
                var type = lines[i].Type;
                if (type != TextDiffLineType.Added && type != TextDiffLineType.Deleted)
                    return false;
            }
            return true;
        }

        // Same rule as git's --color-moved: a block must contain at least 20 alphanumeric characters,
        // so moved braces or blank lines do not get flagged.
        private static bool IsSignificantBlock(string[] normalized, int start, int length)
        {
            var alnum = 0;
            for (var i = start; i < start + length; i++)
            {
                foreach (var c in normalized[i])
                {
                    if (char.IsLetterOrDigit(c) && ++alnum >= MIN_MOVED_ALNUM)
                        return true;
                }
            }
            return false;
        }
        #endregion

        #region Word-level highlights
        private static void HighlightBlock(List<TextDiffLine> deleted, List<TextDiffLine> added)
        {
            if (deleted.Count > 0 && added.Count > 0)
            {
                var left = deleted.FindAll(l => !l.IsMoved);
                var right = added.FindAll(l => !l.IsMoved);
                foreach (var (d, a) in PairBySimilarity(left, right))
                    AddInlineHighlights(left[d], right[a]);
            }

            deleted.Clear();
            added.Clear();
        }

        // Aligns the two sides in order (no crossing pairs) to maximise the total similarity of the
        // chosen pairs, ignoring pairs that are too different to be edits of the same line.
        private static List<(int, int)> PairBySimilarity(List<TextDiffLine> deleted, List<TextDiffLine> added)
        {
            var pairs = new List<(int, int)>();
            var n = deleted.Count;
            var m = added.Count;
            if (n == 0 || m == 0)
                return pairs;

            if ((long)n * m > MAX_PAIRING_CELLS)
            {
                if (n == m)
                {
                    for (var i = 0; i < n; i++)
                        pairs.Add((i, i));
                }
                return pairs;
            }

            var leftGrams = new Dictionary<int, int>[n];
            var rightGrams = new Dictionary<int, int>[m];
            for (var i = 0; i < n; i++)
                leftGrams[i] = Bigrams(deleted[i].Content);
            for (var j = 0; j < m; j++)
                rightGrams[j] = Bigrams(added[j].Content);

            var score = new double[n + 1, m + 1];
            var similarity = new double[n, m];
            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var sim = Similarity(deleted[i - 1].Content, added[j - 1].Content, leftGrams[i - 1], rightGrams[j - 1]);
                    similarity[i - 1, j - 1] = sim;

                    var best = Math.Max(score[i - 1, j], score[i, j - 1]);
                    if (sim >= MIN_PAIR_SIMILARITY)
                        best = Math.Max(best, score[i - 1, j - 1] + sim);
                    score[i, j] = best;
                }
            }

            int x = n, y = m;
            while (x > 0 && y > 0)
            {
                var sim = similarity[x - 1, y - 1];
                if (sim >= MIN_PAIR_SIMILARITY && Math.Abs(score[x, y] - (score[x - 1, y - 1] + sim)) < 1e-9)
                {
                    pairs.Add((x - 1, y - 1));
                    x--;
                    y--;
                }
                else if (score[x - 1, y] >= score[x, y - 1])
                {
                    x--;
                }
                else
                {
                    y--;
                }
            }

            pairs.Reverse();
            return pairs;
        }

        private static Dictionary<int, int> Bigrams(string text)
        {
            var grams = new Dictionary<int, int>();
            var s = text.Trim();
            for (var i = 0; i + 1 < s.Length; i++)
            {
                var key = (s[i] << 16) | s[i + 1];
                grams[key] = grams.TryGetValue(key, out var c) ? c + 1 : 1;
            }
            return grams;
        }

        // Dice coefficient over character bigrams of the trimmed lines.
        private static double Similarity(string a, string b, Dictionary<int, int> ga, Dictionary<int, int> gb)
        {
            var ta = a.Trim();
            var tb = b.Trim();
            if (ta.Length < 2 || tb.Length < 2)
                return ta.Length > 0 && ta == tb ? 1.0 : 0.0;

            var common = 0;
            foreach (var (key, countA) in ga)
            {
                if (gb.TryGetValue(key, out var countB))
                    common += Math.Min(countA, countB);
            }

            return 2.0 * common / ((ta.Length - 1) + (tb.Length - 1));
        }

        private static void AddInlineHighlights(TextDiffLine left, TextDiffLine right)
        {
            if (left.Content.Length > MAX_INLINE_CONTENT_LENGTH || right.Content.Length > MAX_INLINE_CONTENT_LENGTH)
                return;

            var chunks = TextInlineChange.Compare(left.Content, right.Content);
            if (chunks.Count > MAX_INLINE_CHUNKS_PER_LINE)
                return;

            foreach (var chunk in chunks)
            {
                if (chunk.DeletedCount > 0)
                    left.Highlights.Add(new TextRange(chunk.DeletedStart, chunk.DeletedCount));

                if (chunk.AddedCount > 0)
                    right.Highlights.Add(new TextRange(chunk.AddedStart, chunk.AddedCount));
            }
        }
        #endregion

        private const int MIN_MOVED_ALNUM = 20;
        private const int MAX_MOVE_CANDIDATES = 64;
        private const int MAX_MOVE_DETECTION_LINES = 20000;
        private const double MIN_PAIR_SIMILARITY = 0.5;
        private const long MAX_PAIRING_CELLS = 40000;
        private const int MAX_INLINE_CONTENT_LENGTH = 1024;
        private const int MAX_INLINE_CHUNKS_PER_LINE = 16;
    }
}

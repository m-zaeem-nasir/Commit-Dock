Name: commitdock
Version: %_version
Release: 1
Summary: Commit Dock - open-source and free Git GUI client
License: MIT
URL: https://github.com/mzaeemnasir/Commit-Dock
Source: https://github.com/mzaeemnasir/Commit-Dock/archive/refs/tags/v%_version.tar.gz
Requires: libX11.so.6()(%{__isa_bits}bit)
Requires: libSM.so.6()(%{__isa_bits}bit)
Requires: libicu
Requires: xdg-utils

%define _build_id_links none

%description
Commit Dock is an open-source and free Git GUI client.

%install
mkdir -p %{buildroot}/opt/commitdock
mkdir -p %{buildroot}/%{_bindir}
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons
cp -f %{_topdir}/../../CommitDock/* %{buildroot}/opt/commitdock/
ln -rsf %{buildroot}/opt/commitdock/commitdock %{buildroot}/%{_bindir}
cp -r %{_topdir}/../_common/applications %{buildroot}/%{_datadir}
cp -r %{_topdir}/../_common/icons %{buildroot}/%{_datadir}
chmod 755 -R %{buildroot}/opt/commitdock
chmod 755 %{buildroot}/%{_datadir}/applications/commitdock.desktop

%files
%dir /opt/commitdock/
/opt/commitdock/*
/usr/share/applications/commitdock.desktop
/usr/share/icons/*
%{_bindir}/commitdock

%changelog
# skip

; Installateur Windows de SecondMonitor : installe les deux applications.
;   SecondMonitor            l'application qui affiche les données (chronométrage, stratégie, ingénieur IA)
;   SecondMonitor Collecteur la petite application du PC de jeu qui lit le jeu et envoie les données
;
; Construit par construire.sh (ou GitHub Actions) :
;   iscc /DVersion=0.4.0 /DSource=dist\windows-x64\SecondMonitor /DSortie=dist packaging\windows\SecondMonitor.iss
; Pas besoin d'être administrateur : installation dans le dossier de l'utilisateur (on peut choisir « pour tous »).

#ifndef Version
  #define Version "0.0.0"
#endif
#ifndef Source
  #define Source "..\..\dist\windows-x64\SecondMonitor"
#endif
#ifndef Sortie
  #define Sortie "..\..\dist"
#endif

[Setup]
AppId={{6E4C1B7A-2F3D-4C8E-9A51-5D0B7C3E9F21}
AppName=SecondMonitor
AppVersion={#Version}
AppVerName=SecondMonitor {#Version}
AppPublisher=OneDrop3D
AppPublisherURL=https://github.com/OneDrop3D/SecondMonitor
AppSupportURL=https://github.com/OneDrop3D/SecondMonitor/issues
DefaultDirName={autopf}\SecondMonitor
DefaultGroupName=SecondMonitor
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#Sortie}
OutputBaseFilename=SecondMonitor-{#Version}-windows-x64-installation
SetupIconFile=..\SecondMonitor.ico
UninstallDisplayIcon={app}\SecondMonitorHub.exe
UninstallDisplayName=SecondMonitor
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
LicenseFile=..\..\LICENSE

[Languages]
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"

[Types]
Name: "complet"; Description: "Les deux applications"
Name: "jeu"; Description: "PC de jeu seulement : le collecteur"
Name: "ecran"; Description: "Ordinateur qui affiche seulement : SecondMonitor"
Name: "perso"; Description: "Choisir"; Flags: iscustom

[Components]
Name: "visualiseur"; Description: "SecondMonitor : affiche le chronométrage, la stratégie, l'ingénieur IA"; Types: complet ecran perso
Name: "collecteur"; Description: "SecondMonitor Collecteur : lit le jeu et envoie les données (PC de jeu)"; Types: complet jeu perso

[Tasks]
Name: "bureau"; Description: "Créer les raccourcis sur le Bureau"; GroupDescription: "Raccourcis :"
Name: "demarrage"; Description: "Lancer le collecteur avec Windows (fenêtre réduite)"; GroupDescription: "Collecteur :"; Components: collecteur; Flags: unchecked

[Files]
; Les deux applications partagent le même .NET (un seul exemplaire dans le dossier).
Source: "{#Source}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\SecondMonitor"; Filename: "{app}\SecondMonitorHub.exe"; WorkingDir: "{app}"; Components: visualiseur
Name: "{autoprograms}\SecondMonitor Collecteur"; Filename: "{app}\SecondMonitor-Collecteur.exe"; WorkingDir: "{app}"; Components: collecteur
Name: "{autodesktop}\SecondMonitor"; Filename: "{app}\SecondMonitorHub.exe"; WorkingDir: "{app}"; Components: visualiseur; Tasks: bureau
Name: "{autodesktop}\SecondMonitor Collecteur"; Filename: "{app}\SecondMonitor-Collecteur.exe"; WorkingDir: "{app}"; Components: collecteur; Tasks: bureau

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SecondMonitor Collecteur"; ValueData: """{app}\SecondMonitor-Collecteur.exe"" --reduit"; Tasks: demarrage; Flags: uninsdeletevalue

[Run]
Filename: "{app}\SecondMonitor-Collecteur.exe"; Description: "Ouvrir SecondMonitor Collecteur"; Components: collecteur; Flags: nowait postinstall skipifsilent
Filename: "{app}\SecondMonitorHub.exe"; Description: "Ouvrir SecondMonitor"; Components: visualiseur; Flags: nowait postinstall skipifsilent unchecked

[Code]
// Le démarrage automatique peut aussi être activé depuis la fenêtre du collecteur : on l'enlève à la désinstallation.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'SecondMonitor Collecteur');
end;

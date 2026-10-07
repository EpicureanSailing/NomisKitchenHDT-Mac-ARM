#!/usr/bin/env python3
"""Export only allowlisted public files and audit every archived byte."""
import getpass
import hashlib
from pathlib import Path
import re
import socket
import zipfile

ROOT = Path(__file__).resolve().parents[1]
NAME = 'NomiDance-macOS-arm64-v0.2.0'
TOP = ('README.md', 'LICENSE', 'THIRD_PARTY.md', 'RELEASE_NOTES.md', '.gitignore')
TREES = ('src', 'vendor', 'licenses', 'scripts', 'installer', 'docs')
EXTENSIONS = {'.c', '.h', '.cs', '.csproj', '.sh', '.py', '.md', '.txt', '.command', '.lua', '.in'}

def audit(name, data):
    if any(part in {'__MACOSX', '.git', '__pycache__', 'bin', 'obj'} for part in Path(name).parts):
        raise ValueError('Forbidden archive path: ' + name)
    if name.lower().endswith(('.log', '.pdb', '.mdb', '.original-backup')):
        raise ValueError('Forbidden file: ' + name)
    # Values come from the local machine only, never from checked-in config.
    private = [str(Path.home()), getpass.getuser(), socket.gethostname()]
    for value in private:
        if len(value) >= 4 and any(value.encode(enc).lower() in data.lower()
                                   for enc in ('utf-8', 'utf-16-le')):
            raise ValueError('Local identity/path detected in: ' + name)
    for text in (data.decode('utf-8', errors='ignore'), data.decode('utf-16-le', errors='ignore')):
        if re.search('/' + r'(?:Users|home)/[^\s/]+|[A-Z]:\\' + r'Users\\', text):
            raise ValueError('Private filesystem path detected in: ' + name)
        if re.search(r'(?:gh[pousr]_|github_pat_)[A-Za-z0-9_]{20,}|-----BEGIN [A-Z ]*PRIVATE KEY-----', text):
            raise ValueError('Potential credential detected in: ' + name)

def source_files():
    result = [ROOT / name for name in TOP]
    for tree in TREES:
        for path in sorted((ROOT / tree).rglob('*')):
            if path.is_symlink():
                raise ValueError('Symlinks are not exported: ' + str(path.relative_to(ROOT)))
            if not path.is_file() or set(path.relative_to(ROOT).parts) & {'bin', 'obj', '__pycache__', 'payload'}:
                continue
            if path.suffix in EXTENSIONS:
                result.append(path)
    return result

def write_zip(destination, files):
    with zipfile.ZipFile(destination, 'w', zipfile.ZIP_DEFLATED) as archive:
        for name, path in files:
            data = path.read_bytes()
            audit(name, data)
            info = zipfile.ZipInfo(name, (2026, 10, 7, 0, 0, 0))
            info.create_system = 3
            info.external_attr = (0o100755 if path.suffix in ('.sh', '.command') or path.name == 'Hearthstone' else 0o100644) << 16
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)
    # Re-open and audit what will actually be uploaded, including binary strings.
    with zipfile.ZipFile(destination) as archive:
        for name in archive.namelist():
            audit(name, archive.read(name))

def main():
    dist = ROOT / 'dist'
    dist.mkdir(exist_ok=True)
    files = source_files()
    source = [(NAME + '-source/' + str(p.relative_to(ROOT)), p) for p in files]
    # Bundle corresponding modified sources in the installable archive as well.
    package = [(NAME + '/source/' + str(p.relative_to(ROOT)), p) for p in files]
    for path in files:
        rel = path.relative_to(ROOT)
        if rel.parts[0] == 'installer':
            package.append((NAME + '/' + str(Path(*rel.parts[1:])), path))
        elif rel.parts[0] == 'licenses' or str(rel) in TOP:
            package.append((NAME + '/' + str(rel), path))
    for filename in ('Hearthstone', 'libdoorstop.dylib', 'BepInEx.Preloader.dll', 'NomiDance.Status.dll', 'SHA256SUMS'):
        path = ROOT / 'build/payload' / filename
        if not path.is_file():
            raise SystemExit('Missing payload. Run scripts/build.sh first.')
        package.append((NAME + '/payload/' + filename, path))
    artifacts = [dist / (NAME + '.zip'), dist / (NAME + '-source.zip')]
    write_zip(artifacts[0], package)
    write_zip(artifacts[1], source)
    (dist / 'SHA256SUMS').write_text(''.join(hashlib.sha256(p.read_bytes()).hexdigest() + '  ' + p.name + '\n' for p in artifacts))
    print('Privacy audit passed: allowlisted sources and payload only; no local identity, paths, logs or credentials detected.')
    for path in artifacts:
        print(path.name + ': ' + str(path.stat().st_size) + ' bytes')

if __name__ == '__main__':
    main()

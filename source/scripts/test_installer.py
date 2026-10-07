#!/usr/bin/env python3
"""Exercise a fake app only. Never modify or close the real Hearthstone."""
import os
from pathlib import Path
import plistlib
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]

class InstallerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='nomi-installer-test-')
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name).resolve()
        self.app = self.base / 'Game with spaces/Hearthstone.app'
        self.binary = self.app / 'Contents/MacOS/Hearthstone'
        self.binary.parent.mkdir(parents=True)
        framework = self.app / 'Contents/Frameworks'
        framework.mkdir()
        (framework / 'UnityPlayer.dylib').write_bytes(b'fixture Unity 6000.3.11f1')
        (self.app / 'Contents/Info.plist').write_bytes(plistlib.dumps({'CFBundleExecutable': 'Hearthstone'}))
        source = self.base / 'fake.c'
        source.write_text('#include <stdio.h>\n#include <stdlib.h>\nint main(int n,char **v){'
                          'printf("%s\\n%s\\n%s\\n%s\\n",v[0],n>1?v[1]:"",'
                          'getenv("DOORSTOP_ENABLED")?getenv("DOORSTOP_ENABLED"):"unset",'
                          'getenv("DOORSTOP_TARGET_ASSEMBLY")?getenv("DOORSTOP_TARGET_ASSEMBLY"):"unset");return 0;}')
        subprocess.run(['clang', '-arch', 'arm64', str(source), '-o', str(self.binary)], check=True, capture_output=True)
        self.original = self.binary.read_bytes()
        self.runtime = self.app.parent / '.nomi-dance-arm64'
        self.backup = self.binary.with_name('Hearthstone.nomi-original')
        self.harness = self.base / 'package'
        shutil.copytree(ROOT / 'installer', self.harness)
        shutil.copytree(ROOT / 'build/payload', self.harness / 'payload')
        shutil.copytree(ROOT / 'licenses', self.harness / 'licenses')
        # Isolate only process discovery, so a real running game is never touched.
        guard = self.base / 'pgrep-fixture'
        guard.write_text('#!/bin/bash\n'
                         'if [[ -n "${FIXTURE_COUNTER_FILE:-}" ]]; then\n'
                         'n=$(cat "$FIXTURE_COUNTER_FILE"); n=$((n+1)); echo "$n" > "$FIXTURE_COUNTER_FILE"\n'
                         'if [[ "$n" -ge 3 ]]; then exit 0; fi\nfi\n'
                         'exit "${FIXTURE_PGREP_STATUS:-1}"\n')
        guard.chmod(0o755)
        mover = self.base / 'mv-fixture'
        mover.write_text('#!/bin/bash\n'
                         'if [[ "${FIXTURE_FAIL_COMMIT:-0}" == 1 && "${@: -1}" == */.nomi-dance-arm64 ]]; then exit 1; fi\n'
                         'if [[ "${FIXTURE_FAIL_RELAY:-0}" == 1 && "$1" == */.nomi-arm64-stage.*/Hearthstone ]]; then exit 1; fi\n'
                         'exec /bin/mv "$@"\n')
        mover.chmod(0o755)
        manager = self.harness / 'manage.sh'
        manager.write_text(manager.read_text().replace('/usr/bin/pgrep', '"' + str(guard) + '"')
                           .replace('/bin/mv', '"' + str(mover) + '"'))
        self.env = dict(os.environ, NOMI_DOWNLOAD_CACHE=str(ROOT / 'build/downloads'))

    def run_manager(self, mode, success=True, **extra):
        result = subprocess.run(['/usr/bin/arch', '-arm64', '/bin/bash', str(self.harness / 'manage.sh'), mode, str(self.app)],
                                env=dict(self.env, **extra), capture_output=True, text=True)
        self.assertEqual(result.returncode == 0, success, result.stdout + result.stderr)
        return result

    def assert_untouched(self):
        self.assertEqual(self.binary.read_bytes(), self.original)
        self.assertFalse(self.backup.exists())
        self.assertFalse(self.runtime.exists())

    def test_install_relay_and_restore(self):
        self.run_manager('install')
        self.assertEqual(self.backup.read_bytes(), self.original)
        self.assertTrue((self.runtime / 'BepInEx/plugins/com.community.hs.NomiCantDance.dll').is_file())
        self.assertEqual((self.runtime / 'BepInEx/plugins/NomiDance.Status.dll').read_bytes(),
                         (self.harness / 'payload/NomiDance.Status.dll').read_bytes())
        self.assertIn('LogFixes = false', (self.runtime / 'BepInEx/config/com.community.hs.NomiCantDance.cfg').read_text())
        # A harmless dylib isolates relay testing from Unity initialization.
        noop = self.base / 'noop.c'
        noop.write_text('void noop(void) {}\n')
        subprocess.run(['clang', '-arch', 'arm64', '-dynamiclib', str(noop), '-o', str(self.runtime / 'libdoorstop.dylib')], check=True, capture_output=True)
        result = subprocess.run([str(self.binary), 'argument with spaces'], check=True, capture_output=True, text=True)
        lines = result.stdout.splitlines()
        self.assertEqual(lines[:3], [str(self.binary), 'argument with spaces', '1'])
        self.assertEqual(lines[3], str(self.runtime / 'BepInEx/core/BepInEx.Preloader.dll'))
        (self.runtime / 'disabled').touch()
        result = subprocess.run([str(self.binary)], check=True, capture_output=True, text=True)
        self.assertEqual(result.stdout.splitlines()[2], 'unset')
        self.run_manager('uninstall')
        self.assert_untouched()

    def test_existing_install_refused(self):
        self.run_manager('install')
        installed = self.binary.read_bytes()
        self.run_manager('install', success=False)
        self.assertEqual(self.binary.read_bytes(), installed)
        self.assertEqual(self.backup.read_bytes(), self.original)

    def test_updated_game_preserved(self):
        self.run_manager('install')
        self.binary.write_bytes(b'fixture updated game')
        self.run_manager('uninstall', success=False)
        self.assertEqual(self.binary.read_bytes(), b'fixture updated game')
        self.assertEqual(self.backup.read_bytes(), self.original)

    def test_already_restored_original_cleanup(self):
        self.run_manager('install')
        self.binary.write_bytes(self.original)
        before = self.binary.stat().st_mtime_ns
        self.run_manager('uninstall')
        self.assert_untouched()
        self.assertEqual(self.binary.stat().st_mtime_ns, before)

    def test_corrupt_backup_refused(self):
        self.run_manager('install')
        self.backup.write_bytes(b'fixture corrupt backup')
        installed = self.binary.read_bytes()
        self.run_manager('uninstall', success=False)
        self.assertEqual(self.binary.read_bytes(), installed)

    def test_process_guard(self):
        for status in ('0', '2'):
            self.run_manager('install', success=False, FIXTURE_PGREP_STATUS=status)
            self.assert_untouched()

    def test_wrong_unity_version(self):
        (self.app / 'Contents/Frameworks/UnityPlayer.dylib').write_bytes(b'fixture other version')
        self.run_manager('install', success=False)
        self.assert_untouched()

    def test_corrupt_payload_refused(self):
        (self.harness / 'payload/Hearthstone').write_bytes(b'fixture corrupt payload')
        self.run_manager('install', success=False)
        self.assert_untouched()

    def test_failed_commit_rolls_back(self):
        self.run_manager('install', success=False, FIXTURE_FAIL_COMMIT='1')
        self.assert_untouched()
        self.assertFalse(list(self.app.parent.glob('.nomi-arm64-stage.*')))

    def test_failed_relay_commit_rolls_back(self):
        self.run_manager('install', success=False, FIXTURE_FAIL_RELAY='1')
        self.assert_untouched()
        self.assertFalse(list(self.app.parent.glob('.nomi-arm64-stage.*')))

    def test_download_checksum_failure(self):
        cache = self.base / 'corrupt-cache'
        cache.mkdir()
        (cache / 'bepinex.zip').write_bytes(b'fixture corrupt download')
        result = self.run_manager('install', success=False, NOMI_DOWNLOAD_CACHE=str(cache))
        self.assertIn('SHA-256 incorrect', result.stderr)
        self.assert_untouched()

    def test_game_starts_during_preparation(self):
        counter = self.base / 'counter'
        counter.write_text('0')
        result = self.run_manager('install', success=False, FIXTURE_COUNTER_FILE=str(counter))
        self.assertIn('Fermer Hearthstone', result.stderr)
        self.assert_untouched()

    def test_privacy_audit_rejects_local_data(self):
        from package import audit
        with self.assertRaises(ValueError):
            audit('fixture.txt', str(Path.home()).encode())
        with self.assertRaises(ValueError):
            audit('fixture.log', b'fixture')

if __name__ == '__main__':
    unittest.main(verbosity=2)

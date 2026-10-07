#!/usr/bin/env python3
"""Test Battle.net preparation without opening apps or changing a game."""
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]

class EntryPointTests(unittest.TestCase):
    def run_fixture(self, process_status, open_status=0):
        with tempfile.TemporaryDirectory(prefix='nomi-entrypoint-') as directory:
            base = Path(directory)
            for name, body in {
                'pgrep': 'exit "$PROCESS_STATUS"',
                'open': 'echo opened >> "$EVENTS"; exit "$OPEN_STATUS"',
                'manage.sh': 'echo installed >> "$EVENTS"',
            }.items():
                path = base / name
                path.write_text('#!/bin/bash\n' + body + '\n')
                path.chmod(0o755)
            script = (ROOT / 'installer/Installer.command').read_text()
            script = script.replace('/usr/bin/pgrep', '"' + str(base / 'pgrep') + '"')
            script = script.replace('/usr/bin/open', '"' + str(base / 'open') + '"')
            entry = base / 'Installer.command'
            entry.write_text(script)
            events = base / 'events'
            result = subprocess.run(['/bin/bash', str(entry)], input='\n\n', text=True,
                                    capture_output=True, env=dict(os.environ, PROCESS_STATUS=str(process_status),
                                                                 OPEN_STATUS=str(open_status), EVENTS=str(events)))
            return result, events.read_text().splitlines() if events.exists() else []

    def test_opens_closed_battlenet(self):
        result, events = self.run_fixture(1)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(events, ['opened', 'installed'])
        self.assertIn('Quand Battle.net est prêt', result.stdout)

    def test_keeps_running_battlenet(self):
        result, events = self.run_fixture(0)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(events, ['installed'])

    def test_open_failure_prevents_install(self):
        result, events = self.run_fixture(1, 1)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(events, ['opened'])

    def test_process_error_prevents_install(self):
        result, events = self.run_fixture(2)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(events, [])

if __name__ == '__main__':
    unittest.main(verbosity=2)

#!/usr/bin/env python3
"""Create a ZIP that both Godot's project importer and desktop tools can extract."""

import argparse
from pathlib import Path, PurePosixPath
import stat
import subprocess
import zipfile


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", nargs="?", type=Path,
                        default=root / "KleinesSpiel_con_assets.zip")
    output = parser.parse_args().output
    tracked = subprocess.check_output(
        ["git", "ls-files", "-z", "--", "KleinesSpiel/"], cwd=root
    ).decode().split("\0")
    files = sorted(name for name in tracked if name and not name.endswith(".import"))
    if "KleinesSpiel/project.godot" not in files:
        raise SystemExit("The tracked project.godot is missing.")
    directories = {parent.as_posix() + "/" for name in files
                   for parent in PurePosixPath(name).parents if str(parent) != "."}
    # Godot 4.3 creates folders only when it encounters directory entries.
    # Every parent must therefore precede the files it contains.
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        for name in sorted(directories):
            info = zipfile.ZipInfo(name)
            info.create_system = 3
            info.external_attr = ((stat.S_IFDIR | 0o755) << 16) | 0x10
            archive.writestr(info, b"")
        for name in files:
            archive.write(root / name, name)
    with zipfile.ZipFile(output) as archive:
        if archive.testzip() is not None:
            raise SystemExit("ZIP integrity check failed.")
        seen = set()
        for entry in archive.infolist():
            parent = PurePosixPath(entry.filename.rstrip("/")).parent
            if str(parent) != "." and parent.as_posix() + "/" not in seen:
                raise SystemExit(f"Parent directory missing before {entry.filename}")
            seen.add(entry.filename)
    print(f"Created {output}: {len(files)} files, {len(directories)} directories.")


if __name__ == "__main__":
    main()

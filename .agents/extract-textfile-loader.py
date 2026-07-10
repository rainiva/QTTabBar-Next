#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Byte-preserving extraction (latin1 round-trip) of the text-file reading
# cluster out of ThumbnailTooltipForm.cs into a nested static TextFileLoader.
import io, sys

SRC = r"d:\Project\QTTabBar-Next\QTTabBar\ThumbnailTooltipForm.cs"
DST = r"d:\Project\QTTabBar-Next\QTTabBar\ThumbnailTooltipForm.TextFileLoader.cs"
ENC = "iso-8859-1"

with io.open(SRC, "r", encoding=ENC, newline="") as f:
    raw = f.read()

crlf = "\r\n" in raw
lines = raw.split("\r\n") if crlf else raw.split("\n")

def get(n):
    return lines[n - 1]

def guard(n, needle):
    if needle not in get(n):
        sys.exit("GUARD FAIL line %d: expected %r got %r" % (n, needle, get(n)))

guard(306, "private static string FormatSize(long size)")
guard(314, "return str;")
guard(389, "LoadTextFile(string path, int count")
guard(468, "return Encoding.Default.GetString(buffer);")
guard(473, "LoadTextFile2(string path")
guard(522, "return encoding.GetString(buffer);")

R = [(306, 315), (389, 469), (473, 523)]

moved = []
for (a, b) in R:
    moved.extend(lines[a - 1:b])
    moved.append("")

for (a, b) in sorted(R, key=lambda x: -x[0]):
    del lines[a - 1:b]

# Qualify outer-member references inside the MOVED bodies.
moved_txt = "\n".join(moved)
for old, new in [
    ("int count = MAX_TEXT_LENGTH;", "int count = ThumbnailTooltipForm.MAX_TEXT_LENGTH;"),
    ("ioException = exception;", "ThumbnailTooltipForm.ioException = exception;"),
]:
    if moved_txt.count(old) == 0:
        sys.exit("MOVED REPL FAIL: %r" % old)
    moved_txt = moved_txt.replace(old, new)
moved = moved_txt.split("\n")

main_text = ("\r\n" if crlf else "\n").join(lines)

# Fix the two live callers in CreateThumbnail.
for old, new in [
    ("text = text + FormatSize(info.Length);",
     "text = text + TextFileLoader.FormatSize(info.Length);"),
    ("content = LoadTextFile3(path, out fLoadedAll);",
     "content = TextFileLoader.LoadTextFile3(path, out fLoadedAll);"),
]:
    if main_text.count(old) == 0:
        sys.exit("CALLER REPL FAIL: %r" % old)
    main_text = main_text.replace(old, new)

with io.open(SRC, "w", encoding=ENC, newline="") as f:
    f.write(main_text)

header = """//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2021  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Text;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class ThumbnailTooltipForm {
        // Batch10 GC10b: text-file reading cluster extracted from the
        // ThumbnailTooltipForm god class into this nested static helper.
        private static class TextFileLoader {
"""
footer = "        }\n    }\n}\n"

nl = "\r\n" if crlf else "\n"
body = nl.join(moved)
out = header.replace("\n", nl) + body + nl + footer.replace("\n", nl)

with io.open(DST, "w", encoding=ENC, newline="") as f:
    f.write(out)

print("OK: main lines now", len(lines), "moved lines", len(moved))

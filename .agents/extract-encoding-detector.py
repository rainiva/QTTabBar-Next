#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Byte-preserving extraction (latin1 round-trip) of the charset-detection
# cluster out of ThumbnailTooltipForm.cs into a nested static EncodingDetector.
# latin1 maps every byte 0..255 to a codepoint and round-trips exactly, so the
# pre-corrupted (non-UTF8/non-GBK) comment bytes are preserved verbatim.
import io, sys

SRC = r"d:\Project\QTTabBar-Next\QTTabBar\ThumbnailTooltipForm.cs"
DST = r"d:\Project\QTTabBar-Next\QTTabBar\ThumbnailTooltipForm.EncodingDetector.cs"
ENC = "iso-8859-1"

with io.open(SRC, "r", encoding=ENC, newline="") as f:
    raw = f.read()

# Normalise on \n while remembering CRLF for writing back.
crlf = "\r\n" in raw
lines = raw.split("\r\n") if crlf else raw.split("\n")
# lines[i] is 1-based line (i+1). Trailing element may be "" if file ends with newline.

def get(n):  # 1-based
    return lines[n - 1]

# ---- boundary guards (abort if the file drifted) ----
def guard(n, needle):
    if needle not in get(n):
        sys.exit("GUARD FAIL line %d: expected %r got %r" % (n, needle, get(n)))

guard(61, "_utf16BeBom")
guard(78, "};")
guard(88, "Encoding950")
guard(99, "_utf16UnexpectedNullPercent")
guard(422, "public static System.Text.Encoding GetType(string")
guard(477, "IsUTF8Bytes")
guard(518, "}")
guard(603, "public static Encoding DetectEncoding(byte[] bytes)")
guard(1276, "End Function BomInfo")

# ranges (1-based inclusive)
R = [(61, 78), (88, 99), (417, 518), (603, 1276)]

def slice_lines(a, b):
    return lines[a - 1:b]

moved = []
for (a, b) in R:
    moved.extend(slice_lines(a, b))
    moved.append("")  # blank separator between blocks

# Remove ranges from main (descending so indices stay valid)
for (a, b) in sorted(R, key=lambda x: -x[0]):
    del lines[a - 1:b]

main_text = ("\r\n" if crlf else "\n").join(lines)

# Fix outer callers of the now-moved charset methods.
repl = [
    ("new StreamReader(path, GetType(path))",
     "new StreamReader(path, EncodingDetector.GetType(path))"),
    ("detechted = TryGetEncoding(buffer);",
     "detechted = EncodingDetector.TryGetEncoding(buffer);"),
    ("detechted = DetectEncoding(buffer);",
     "detechted = EncodingDetector.DetectEncoding(buffer);"),
]
for old, new in repl:
    cnt = main_text.count(old)
    if cnt == 0:
        sys.exit("REPL FAIL: pattern not found: %r" % old)
    main_text = main_text.replace(old, new)

with io.open(SRC, "w", encoding=ENC, newline="") as f:
    f.write(main_text)

# Build the EncodingDetector partial file.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using MultiLanguage;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class ThumbnailTooltipForm {
        // Batch10 GC10c: charset-detection cluster extracted from the
        // ThumbnailTooltipForm god class into this nested static helper. Pure
        // static utility; no dependency on form instance state.
        private static class EncodingDetector {
"""
footer = "        }\n    }\n}\n"

nl = "\r\n" if crlf else "\n"
body = nl.join(moved)
out = header.replace("\n", nl) + body + nl + footer.replace("\n", nl)

with io.open(DST, "w", encoding=ENC, newline="") as f:
    f.write(out)

print("OK: main lines now", len(lines), "moved lines", len(moved))

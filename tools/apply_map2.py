#!/usr/bin/env python3
"""Bản đồ ngang: đường ải zigzag trên nền thủy mặc + bảng thông tin bên dưới. Chạy lại nhiều lần không sao.
python3 tools/apply_map2.py wwwroot/index.html obj/map2.css obj/map2.js"""
import sys
html, css, js = sys.argv[1:4]
s = open(html, encoding='utf-8', newline='').read()
nl = '\r\n' if '\r\n' in s else '\n'
css = open(css, encoding='utf-8').read().replace('\r\n', '\n').strip('\n')
js = open(js, encoding='utf-8').read().replace('\r\n', '\n').strip('\n')
# CSS: thay khối cũ hoặc chèn trước </style> cuối cùng của <head>
A, Z = '/* ================= BẢN ĐỒ NGANG', '/* ===== hết BẢN ĐỒ NGANG ===== */'
if A in s:
    a = s.index(A); b = s.index(Z, a) + len(Z)
    s = s[:a] + css.replace('\n', nl) + s[b:]
else:
    k = s.index('</style>' + nl + '</head>')
    s = s[:k] + css.replace('\n', nl) + nl + s[k:]
# JS: thay từ function renderMap(){ đến hết function initMap(){…}
a = s.index('function renderMap(){')
b = s.index('function initMap(){', a)
b = s.index(nl + '}' + nl, b) + len(nl + '}')
s = s[:a] + js.replace('\n', nl) + s[b:]
open(html, 'w', encoding='utf-8', newline='').write(s)
print('ok')

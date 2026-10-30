#!/usr/bin/env python3
"""Reference texture-budget calculator (for the instructor; students write their own)."""
BPP = {'RGBA32': 32, 'ASTC 4×4': 8, 'ASTC 6×6': 128 / 36, 'ASTC 8×8': 2,
       'ETC2 RGB': 4, 'ETC2 RGBA': 8}


def mem(side, fmt, mips=True):
    b = side * side * BPP[fmt] / 8
    if mips:
        b *= 4 / 3
    return b / 2 ** 20


SCENE = [('Ground_Albedo', 2048), ('Ground_Normal', 2048), ('Building_Albedo', 2048), ('Building_Normal', 2048),
         ('Props_Albedo', 1024), ('Props_Normal', 1024), ('Skybox', 2048), ('Character_Albedo', 1024),
         ('Character_Normal', 1024), ('UI_Atlas', 2048), ('Decals', 1024), ('Noise', 512)]

if __name__ == '__main__':
    print('початково: %.2f МБ' % sum(mem(s, 'RGBA32') for _, s in SCENE))

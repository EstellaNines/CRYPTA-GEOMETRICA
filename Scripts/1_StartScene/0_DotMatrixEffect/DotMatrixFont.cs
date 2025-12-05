using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 5x7点阵字体数据
/// 每个字母用bool[7,5]表示（7行5列）
/// true表示该位置有点，false表示无点
/// </summary>
public static class DotMatrixFont
{
    // 点阵数据字典
    private static Dictionary<char, bool[,]> fontData;

    /// <summary>
    /// 初始化字体数据
    /// </summary>
    static DotMatrixFont()
    {
        fontData = new Dictionary<char, bool[,]>();
        InitializeFontData();
    }

    /// <summary>
    /// 获取字符的点阵数据
    /// </summary>
    public static bool[,] GetCharacterMatrix(char c)
    {
        char upperChar = char.ToUpper(c);
        if (fontData.ContainsKey(upperChar))
        {
            return fontData[upperChar];
        }
        return GetCharacterMatrix(' '); // 返回空格
    }

    /// <summary>
    /// 初始化所有字母的点阵数据
    /// </summary>
    private static void InitializeFontData()
    {
        // A
        fontData['A'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, true, true, true, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true }
        };

        // B
        fontData['B'] = new bool[,]
        {
            { true, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, true, true, true, false }
        };

        // C
        fontData['C'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // D
        fontData['D'] = new bool[,]
        {
            { true, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, true, true, true, false }
        };

        // E
        fontData['E'] = new bool[,]
        {
            { true, true, true, true, true },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, true, true, true, false },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, true, true, true, true }
        };

        // F
        fontData['F'] = new bool[,]
        {
            { true, true, true, true, true },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, true, true, true, false },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, false, false, false, false }
        };

        // G
        fontData['G'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, false },
            { true, false, true, true, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // H
        fontData['H'] = new bool[,]
        {
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, true, true, true, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true }
        };

        // I
        fontData['I'] = new bool[,]
        {
            { false, true, true, true, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, true, true, true, false }
        };

        // J
        fontData['J'] = new bool[,]
        {
            { false, false, true, true, true },
            { false, false, false, true, false },
            { false, false, false, true, false },
            { false, false, false, true, false },
            { true, false, false, true, false },
            { true, false, false, true, false },
            { false, true, true, false, false }
        };

        // K
        fontData['K'] = new bool[,]
        {
            { true, false, false, false, true },
            { true, false, false, true, false },
            { true, false, true, false, false },
            { true, true, false, false, false },
            { true, false, true, false, false },
            { true, false, false, true, false },
            { true, false, false, false, true }
        };

        // L
        fontData['L'] = new bool[,]
        {
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, true, true, true, true }
        };

        // M
        fontData['M'] = new bool[,]
        {
            { true, false, false, false, true },
            { true, true, false, true, true },
            { true, false, true, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true }
        };

        // N
        fontData['N'] = new bool[,]
        {
            { true, false, false, false, true },
            { true, true, false, false, true },
            { true, false, true, false, true },
            { true, false, false, true, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true }
        };

        // O
        fontData['O'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // P
        fontData['P'] = new bool[,]
        {
            { true, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, true, true, true, false },
            { true, false, false, false, false },
            { true, false, false, false, false },
            { true, false, false, false, false }
        };

        // Q
        fontData['Q'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, true, false, true },
            { true, false, false, true, false },
            { false, true, true, false, true }
        };

        // R
        fontData['R'] = new bool[,]
        {
            { true, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, true, true, true, false },
            { true, false, true, false, false },
            { true, false, false, true, false },
            { true, false, false, false, true }
        };

        // S
        fontData['S'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, false },
            { false, true, true, true, false },
            { false, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // T
        fontData['T'] = new bool[,]
        {
            { true, true, true, true, true },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false }
        };

        // U
        fontData['U'] = new bool[,]
        {
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // V
        fontData['V'] = new bool[,]
        {
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, false, true, false },
            { false, false, true, false, false }
        };

        // W
        fontData['W'] = new bool[,]
        {
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { true, false, true, false, true },
            { true, false, true, false, true },
            { true, true, false, true, true },
            { true, false, false, false, true }
        };

        // X
        fontData['X'] = new bool[,]
        {
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, false, true, false },
            { false, false, true, false, false },
            { false, true, false, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true }
        };

        // Y
        fontData['Y'] = new bool[,]
        {
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, false, true, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false }
        };

        // Z
        fontData['Z'] = new bool[,]
        {
            { true, true, true, true, true },
            { false, false, false, false, true },
            { false, false, false, true, false },
            { false, false, true, false, false },
            { false, true, false, false, false },
            { true, false, false, false, false },
            { true, true, true, true, true }
        };

        // 数字 0
        fontData['0'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, true, true },
            { true, false, true, false, true },
            { true, true, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // 数字 1
        fontData['1'] = new bool[,]
        {
            { false, false, true, false, false },
            { false, true, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, false, true, false, false },
            { false, true, true, true, false }
        };

        // 数字 2
        fontData['2'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { false, false, false, false, true },
            { false, false, false, true, false },
            { false, false, true, false, false },
            { false, true, false, false, false },
            { true, true, true, true, true }
        };

        // 数字 3
        fontData['3'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { false, false, false, false, true },
            { false, false, true, true, false },
            { false, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // 数字 4
        fontData['4'] = new bool[,]
        {
            { false, false, false, true, false },
            { false, false, true, true, false },
            { false, true, false, true, false },
            { true, false, false, true, false },
            { true, true, true, true, true },
            { false, false, false, true, false },
            { false, false, false, true, false }
        };

        // 数字 5
        fontData['5'] = new bool[,]
        {
            { true, true, true, true, true },
            { true, false, false, false, false },
            { true, true, true, true, false },
            { false, false, false, false, true },
            { false, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // 数字 6
        fontData['6'] = new bool[,]
        {
            { false, false, true, true, false },
            { false, true, false, false, false },
            { true, false, false, false, false },
            { true, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // 数字 7
        fontData['7'] = new bool[,]
        {
            { true, true, true, true, true },
            { false, false, false, false, true },
            { false, false, false, true, false },
            { false, false, true, false, false },
            { false, true, false, false, false },
            { false, true, false, false, false },
            { false, true, false, false, false }
        };

        // 数字 8
        fontData['8'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, false }
        };

        // 数字 9
        fontData['9'] = new bool[,]
        {
            { false, true, true, true, false },
            { true, false, false, false, true },
            { true, false, false, false, true },
            { false, true, true, true, true },
            { false, false, false, false, true },
            { false, false, false, true, false },
            { false, true, true, false, false }
        };

        // 百分号 %
        fontData['%'] = new bool[,]
        {
            { true, true, false, false, false },
            { true, true, false, false, true },
            { false, false, false, true, false },
            { false, false, true, false, false },
            { false, true, false, true, true },
            { true, false, false, true, true },
            { false, false, false, false, false }
        };

        // 空格
        fontData[' '] = new bool[,]
        {
            { false, false, false, false, false },
            { false, false, false, false, false },
            { false, false, false, false, false },
            { false, false, false, false, false },
            { false, false, false, false, false },
            { false, false, false, false, false },
            { false, false, false, false, false }
        };
        
        // 下划线（占据最下方一行）
        fontData['_'] = new bool[,]
        {
            { false, false, false, false, false },
            { false, false, false, false, false },
            { false, false, false, false, false },
            { false, false, false, false, false },
            { false, false, false, false, false },
            { false, false, false, false, false },
            { true, true, true, true, true }
        };
    }
}

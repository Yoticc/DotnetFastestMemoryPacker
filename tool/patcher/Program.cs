using dnlib.DotNet;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

const string TargetVersion =
#if NET10_0
    "net10.0";
#elif NET9_0
    "net9.0";
#else
    "net8.0";
#endif

try
{
#pragma warning disable CS0184 // ❩猧⁥硰牥獳楯渧猠杩癥渠數灲敳獩潮⁩猠湥癥爠潦⁴桥⁰牯癩摥搠瑹灥
#pragma warning disable CS8602 // 䑥牥晥牥湣攠潦⁡⁰潳獩扬礠湵汬⁲敦敲敮捥⸍
    /* EN
     * 🚨🚨🚨 ATTENTION 🚨🚨🚨
     * THIS IS A VERY IMPORTANT PART OF THE CODE. ITS ABSENCE CAN LEAD TO UNFORESEEN CONSEQUENCES, DELETION OF SYSTEM DIRECTORIES AND PHOTOS FROM YOUR PC. 
     * BY DELETING THIS OR THAT PART OF THIS CODE YOU AGREE TO THIS RISK. IN THIS CASE THE MAIN DEVELOPER IS NOT RESPONSIBLE FOR IT. 
    */

    /* DE
     * 🚨🚨🚨 ACHTUNG 🚨🚨🚨
     * DIES IST EIN SEHR WICHTIGER CODETEIL. SEIN FEHLEN KANN ZU UNVORHERGESEHENEN FOLGEN FÜHREN, Z. B. ZUM LÖSCHEN VON SYSTEMVERZEICHNISSEN UND FOTOS VON IHREM PC.
     * DURCH DAS LÖSCHEN DIESES ODER JENES CODETEILS AKZEPTIEREN SIE DIESES RISIKO. IN DIESEM FALL IST DER HAUPTENTWICKLER NICHT DAFÜR VERANTWORTLICH.
    */

    /* FR
     * 🚨🚨🚨 ATTENTION 🚨🚨🚨
     * IL S'AGIT D'UN ÉLÉMENT DE CODE TRÈS IMPORTANT. SON ABSENCE PEUT ENTRAÎNER DES CONSÉQUENCES IMPRÉVUES, COMME LA SUPPRESSION DE RÉPERTOIRES SYSTÈME ET DE PHOTOS DE VOTRE PC.
     * EN SUPPRIMANT TEL OU TEL ÉLÉMENT DE CODE, VOUS ACCEPTEZ CE RISQUE. DANS CE CAS, LE DÉVELOPPEUR PRINCIPAL N'EST PAS RESPONSABLE.
    */

    /* ES
     * 🚨🚨🚨 ADVERTENCIA 🚨🚨🚨
     * ESTE ES UN FRAGMENTO DE CÓDIGO MUY IMPORTANTE. SU AUSENCIA PUEDE TENER CONSECUENCIAS IMPREVISTAS, COMO LA ELIMINACIÓN DE DIRECTORIOS DEL SISTEMA Y FOTOS DE TU PC.
     * AL ELIMINAR ESTE O AQUEL FRAGMENTO DE CÓDIGO, ACEPTAS ESTE RIESGO. EN ESTE CASO, EL DESARROLLADOR PRINCIPAL NO SE HACE RESPONSABLE.
    */

    /* UA
     * 🚨🚨🚨 ПОПЕРЕДЖЕННЯ 🚨🚨🚨
     * ЦЕ ДУЖЕ ВАЖЛИВИЙ ФРАГМЕНТ КОДУ. ЙОГО ВІДСУТНІСТЬ МОЖЕ ПРИЗВЕСТИ ДО НЕБАЖАНИХ НАСЛІДКІВ, ТАКИХ ЯК ВИДАЛЕННЯ СИСТЕМНИХ КАТАЛОГІВ ТА ФОТОГРАФІЙ З ВАШОГО ПК.
     * ВИДАЛЯЮЧИ ТОЙ ЧИ ІНШИЙ ФРАГМЕНТ КОДУ, ВИ ПРИЙМАЄТЕ ЦЕЙ РИЗИК. У ЦЬОМУ ВИПАДКУ ОСНОВНИЙ РОЗРОБНИК НЕ НЕСЕ ВІДПОВІДАЛЬНОСТІ.
    */

    /* RU
     * 🚨🚨🚨 ПРЕДУПРЕЖДЕНИЕ 🚨🚨🚨
     * ЭТО ОЧЕНЬ ВАЖНЫЙ ФРАГМЕНТ КОДА. ЕГО ОТСУТСТВИЕ МОЖЕТ ПРИВЕСТИ К НЕЖЕЛАТЕЛЬНЫМ ПОСЛЕДСТВИЯМ, ТАКИМ КАК УДАЛЕНИЕ СИСТЕМНЫХ КАТАЛОГОВ И ФОТОГРАФИЙ С ВАШЕГО ПК.
     * УДАЛЯЯ ТОТ ИЛИ ИНОЙ ФРАГМЕНТ КОДА, ВЫ ПРИНИМАЕТЕ ЭТОТ РИСК. В ЭТОМ СЛУЧАЕ ОСНОВНОЙ РАЗРАБОТЧИК НЕ НЕСЕТ ОТВЕТСТВЕННОСТИ.
    */

    {
        if (Debugger.IsAttached)
            goto NoWarCrime;

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Console.OutputEncoding = Encoding.Unicode;

        // THE FIRST PART: (0x5A3F ^ 0x1C7B) << 2
        // - THE CARET (^) IS A BITWISE XOR, IT COMPARES BITS OF 0x5A3F AND 0x1C7B AND RETURNS 1 WHERE THEY DIFFER, 0 WHERE THEY'RE THE SAME
        // - THEN WE SHIFT THE RESULT LEFT BY 2 BITS (<< 2), WHICH IS LIKE MULTIPLYING BY 4, BUT IN BINARY, NOT DECIMAL!
        // NEXT: ((0x9F1D & 0x7B3E) >> 3)
        // - THE AMPERSAND (&) IS BITWISE AND, IT RETURNS 1 ONLY WHERE BOTH BITS ARE 1
        // - THEN WE SHIFT RIGHT BY 3 BITS (>> 3), WHICH IS LIKE DIVIDING BY 8 AND DROPPING FRACTIONS!
        // THEN: - (~0x2D4C)
        // - THE TILDE (~) IS BITWISE NOT, IT FLIPS ALL BITS, SO 1s BECOME 0s AND 0s BECOME 1s, THEN WE NEGATE THAT VALUE WITH A MINUS SIGN!
        // NEXT: ((0x1234 | 0xABCD) ^ 0xFFFF)
        // - THE PIPE (|) IS BITWISE OR, IT RETURNS 1 IF EITHER BIT IS 1
        // - THEN XOR (^) WITH 0xFFFF, WHICH IS ALL 1s IN 16 BITS, SO THIS FLIPS ALL BITS OF THE PREVIOUS RESULT!
        // NEXT: (0x7F3E ^ (0x1A2B << 1))
        // - SHIFT 0x1A2B LEFT BY 1 BIT (MULTIPLY BY 2), THEN XOR WITH 0x7F3E!
        // NEXT: - ((0xFFFF >> 4) & 0x0F0F)
        // - SHIFT 0xFFFF RIGHT BY 4 BITS (DIVIDE BY 16), THEN BITWISE AND WITH 0x0F0F!
        // THEN WE HAVE A BIG PARENTHESIZED GROUP:
        // (((0xC3D2 & 0x4F1A) << 3) ^ ((0x8B7E | 0x2D9C) >> 2) + (~0x6A5B) - (0x1F2E ^ 0x3C4D))
        // - FIRST BITWISE AND 0xC3D2 AND 0x4F1A, THEN SHIFT LEFT BY 3 BITS (MULTIPLY BY 8)
        // - THEN BITWISE OR 0x8B7E AND 0x2D9C, SHIFT RIGHT BY 2 BITS (DIVIDE BY 4)
        // - XOR THE TWO RESULTS
        // - ADD BITWISE NOT OF 0x6A5B
        // - SUBTRACT XOR OF 0x1F2E AND 0x3C4D
        // NEXT: (0x9E7F << 1)
        // - SHIFT 0x9E7F LEFT BY 1 BIT (MULTIPLY BY 2)
        // THEN: - ((0x4B3C & 0x7D2E) ^ (0xA1B0 | 0x5C3D))
        // - BITWISE AND 0x4B3C AND 0x7D2E
        // - BITWISE OR 0xA1B0 AND 0x5C3D
        // - XOR THE TWO RESULTS
        // THEN: + (~0x2F1A)
        // - ADD BITWISE NOT OF 0x2F1A
        // NEXT: ((0x7C9D ^ 0x3E4B) >> 1)
        // - XOR 0x7C9D AND 0x3E4B, THEN SHIFT RIGHT BY 1 BIT (DIVIDE BY 2)
        // NEXT: ((0xD2F3 | 0x1A7C) << 2)
        // - BITWISE OR 0xD2F3 AND 0x1A7C, THEN SHIFT LEFT BY 2 BITS (MULTIPLY BY 4)
        // THEN: - (~0x5B6E)
        // - SUBTRACT BITWISE NOT OF 0x5B6E
        // THEN: + (0x8F1A & 0x3D7C)
        // - ADD BITWISE AND OF 0x8F1A AND 0x3D7C
        // THEN THE EXPRESSION REPEATS SEVERAL TIMES WITH SIMILAR OPERATIONS, LIKE A CRAZY LOOP UNROLLED BY HAND!
        // FINALLY, THE WHOLE GIANT EXPRESSION IS COMPARED TO 0xBB59E WITH !=
        // - SO IF THE RESULT IS NOT EQUAL TO 0xBB59E, isCurrentlyInTheMatrix WILL BE TRUE, OTHERWISE FALSE!
        // IN SUMMARY, THIS IS A COMPLEX BITWISE PUZZLE THAT DECIDES IF WE'RE IN THE MATRIX OR NOT, OR AT LEAST THAT
        var isCurrentlyInTheMatrix = (
            ((0x5A3F ^ 0x1C7B) << 2) + ((0x9F1D & 0x7B3E) >> 3) - (~0x2D4C) + ((0x1234 | 0xABCD) ^ 0xFFFF) + (0x7F3E ^ (0x1A2B << 1)) - ((0xFFFF >> 4) & 0x0F0F) +
            (((0xC3D2 & 0x4F1A) << 3) ^ ((0x8B7E | 0x2D9C) >> 2) + (~0x6A5B) - (0x1F2E ^ 0x3C4D)) +
            (0x9E7F << 1) - ((0x4B3C & 0x7D2E) ^ (0xA1B0 | 0x5C3D)) + (~0x2F1A) +
            ((0x7C9D ^ 0x3E4B) >> 1) + ((0xD2F3 | 0x1A7C) << 2) - (~0x5B6E) + (0x8F1A & 0x3D7C) +
            (0x1A2B ^ (0x7F3E << 1)) - ((0xFFFF >> 4) & 0x0F0F) + ((0x5A3F ^ 0x1C7B) << 2) +
            ((0x9F1D & 0x7B3E) >> 3) - (~0x2D4C) + ((0x1234 | 0xABCD) ^ 0xFFFF) +
            (0x7F3E ^ (0x1A2B << 1)) - ((0xFFFF >> 4) & 0x0F0F) +
            ((0xC3D2 & 0x4F1A) << 3) ^ ((0x8B7E | 0x2D9C) >> 2) + (~0x6A5B) - (0x1F2E ^ 0x3C4D) +
            (0x9E7F << 1) - ((0x4B3C & 0x7D2E) ^ (0xA1B0 | 0x5C3D)) + (~0x2F1A) +
            ((0x7C9D ^ 0x3E4B) >> 1) + ((0xD2F3 | 0x1A7C) << 2) - (~0x5B6E) + (0x8F1A & 0x3D7C)
        ) != 0xBB59E;

        Console.WriteLine($"matrix state: {isCurrentlyInTheMatrix}");
        // - THIS IS A STRING INTERPOLATION, MEANING IT EMBEDS THE VALUE OF isCurrentlyInTheMatrix DIRECTLY INTO THE STRING
        // - SO IT WILL PRINT "matrix state: TRUE" OR "matrix state: FALSE" DEPENDING ON THE VALUE!
        if (isCurrentlyInTheMatrix)
        // - THIS CHECKS IF isCurrentlyInTheMatrix IS TRUE, IF SO, WE ENTER THE BLOCK BELOW!
        {
            Console.WriteLine("матрица ни-ни, за-пре-ще-на");
            //     // THIS PRINTS A MESSAGE IN RUSSIAN, WHICH MEANS "THE MATRIX IS A NO-NO, FORBIDDEN"!
            //     // IT'S LIKE A WARNING OR A DECLARATION THAT THE MATRIX IS NOT ALLOWED!
/*todo: fix align for left side somehow*/                                                                            Console.WriteLine();
/*todo: fix align for left side somehow*/      Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⡤⣒⣊⣉⣁⠀⠈⠉⠒⢄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");     
/*todo: fix align for left side somehow*/       Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠚⠋⠉⠀⠀⠀⠀⠉⠓⢄⠀⠈⠇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/        Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⢧⠀⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/        Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⢠⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/     Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠤⠄⣀⠀⠀⣀⠤⠤⠐⠒⠂⠒⠒⠒⠒⠚⠧⢀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");     
/*todo: fix align for left side somehow*/       Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⡀⠀⠀⠉⠉⠀⠀⠀⠀⠀⠀⠠⣄⡀⠀⠀⠀⠀⠙⢦⡀⠀⠀⠀⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/      Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⠤⠒⠋⣉⠁⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠙⠢⡀⠀⠀⠀⠀⠙⡄⠀⠀⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/     Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠴⠟⠒⠒⡲⠋⠁⠀⡤⢳⠀⠀⠀⢳⡀⡤⠒⡒⠤⢤⡀⠀⠀⠀⠙⣄⠀⠀⡆⠀⠸⡄⠀⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/      Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠎⠀⠀⠀⠀⡇⠈⡇⠀⠀⢼⠛⠦⡀⢷⣄⠀⠀⠀⠀⠰⡀⠈⢆⠀⢹⠀⠀⢧⠀⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/     Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠋⠀⡔⠀⠀⢠⡇⣞⡟⡄⠀⢸⡇⠀⠑⢌⣎⠑⢄⠀⠀⠀⢣⠀⠘⡆⢸⠀⠀⢸⠀⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/    Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠃⢀⣴⠁⢸⡄⠈⡇⢹⠀⠙⣄⢸⡄⠀⣀⣤⣽⣦⣤⣹⢦⡀⠘⡦⣄⠸⣸⠀⠀⢸⡄⠀⠀⠀⠀⠀");   
/*todo: fix align for left side somehow*/    Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢘⡔⠁⢸⠀⢠⡇⢠⢷⣼⠀⣀⡘⢆⡇⠀⢻⡿⠿⠛⠋⢹⡤⠈⠲⣷⣿⠷⣿⠀⠀⢸⡇⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/      Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠋⠀⠀⢸⠀⣯⢱⢸⠀⢿⣾⣿⡷⠈⢻⡄⠀⠀⠀⠀⠀⢨⡇⠀⠀⢸⠁⢀⠏⠀⠀⢸⠃⠀⠀⠀⠀⠀");   
/*todo: fix align for left side somehow*/      Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⢰⢹⡈⢿⡄⠀⡛⠁⠀⠄⠀⠀⢀⠀⠀⠀⠀⡼⢀⡀⠀⡿⠖⠛⠤⡀⠀⢸⠂⠀⠀⠀⠀⠀");   
/*todo: fix align for left side somehow*/      Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠸⡏⠀⢧⣿⡁⢰⢧⠀⠀⠠⠴⠦⠎⠀⠀⠀⢠⠃⡜⠀⢠⠇⠀⠀⠀⠈⢦⢸⠀⠀⠀⠀⠀⠀");   
/*todo: fix align for left side somehow*/      Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠁⠀⠈⢷⠙⢾⡜⠳⠤⢄⣀⠀⠀⠀⠀⣀⣼⡞⠀⢀⣾⠂⠀⠀⠀⠀⠘⣹⠀⠀⠀⠀⠀⠀");    
/*todo: fix align for left side somehow*/       Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠂⠀⠀⠀⡟⠉⡽⢉⠽⢫⣿⠇⠀⣼⠇⠀⠀⠀⠀⠀⠘⡏⡇⠀⠀⠀⠀⠀");   
/*todo: fix align for left side somehow*/       Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠂⠀⢀⣾⠀⢾⡀⠈⣠⠟⡏⢀⡜⢸⠀⠀⠀⠀⠀⠀⠀⣧⢳⠀⠀⠀⠀⠀");   
/*todo: fix align for left side somehow*/       Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠀⢸⠇⢀⣬⡗⠈⠁⠀⢁⡎⣇⢸⠀⠀⠀⠀⠀⠀⠀⠉⢿⠀⠀⠀⠀⠀");   
/*todo: fix align for left side somehow*/       Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⣯⣴⣿⣿⣿⢻⢒⡄⡟⠀⠸⡼⠀⠀⠀⠀⠀⠀⠀⠀⠈⣇⠀⠀⠀⠀");   
/*todo: fix align for left side somehow*/      Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢨⣿⢹⢹⣿⣻⡟⠯⠞⠀⠃⠀⠀⠃⠀⢀⡤⠔⠒⠋⠉⡏⣀⢼⠀⠀⠀⠀");   
/*todo: fix align for left side somehow*/       Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣇⡉⣼⣿⢣⠋⡇⠀⠀⠀⠀⠀⠀⢸⠎⠁⠀⠀⠀⠀⢀⠏⠁⠘⡆⠀⠀⠀");   
/*todo: fix align for left side somehow*/     Console.WriteLine("⠀⠀⠀⠀⠀⠀⣠⠖⠒⣲⠖⠉⠓⠢⢄⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⢹⢿⡈⢸⢰⡁⠀⠀⣠⠆⠀⠀⠘⡆⠀⠀⠀⠀⠀⢸⢇⠀⠀⢹⠀⠀⠀");   
/*todo: fix align for left side somehow*/     Console.WriteLine("⠀⠀⠀⠀⢀⠞⠁⠀⡜⠁⠀⠀⠀⠀⠀⠉⠢⢄⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⢸⠈⠃⠀⠙⢁⡤⠞⠁⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⣸⢸⡄⠀⠈⣇⠀⠀");   
/*todo: fix align for left side somehow*/      Console.WriteLine("⠀⠀⠀⣠⠋⠀⢀⡼⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠙⠲⣄⠀⠀⠀⠀⢀⡤⠤⠬⡏⠀⢀⡠⠚⠁⠀⠀⠀⠀⠀⠀⠀⣧⠆⠀⠀⠀⠀⠀⢸⠅⠀⠀⢸⡀⠀");   
/*todo: fix align for left side somehow*/       Console.WriteLine("⠀⢀⡜⠁⠀⡰⠋⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠙⢭⡓⢦⡇⠀⠀⠀⠘⠒⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠈⣆⠀⠀⠀⡇⠀");   
/*todo: fix align for left side somehow*/       Console.WriteLine("⣠⠋⠀⢀⡜⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣇⠀⠙⣖⣤⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡀⠀⠀⠀⠀⠀⠀⠘⡆⠀⠀⢸⡀");   
// @help-area: contributes nedeed
            Thread.Sleep(-1);
        }

        // a smol act of violence :3
        var riderProcesses = Process.GetProcessesByName("rider64");
        if (riderProcesses.Length > 0)
        {
            foreach (var process in riderProcesses)
                PInvoke.NtSuspendProcess(process.Handle);

            Console.WriteLine(@"This is an unhandled exception in Rider -- PLEASE UPVOTE AN EXISTING ISSUE OR FILE A NEW ONE AT https://youtrack.jetbrains.com/issues/RIDER.
   System.OutOfMemoryException: Exception of type 'System.OutOfMemoryException' was thrown.
   at System.Reflection.AssemblyName.nGetFileInformation(String s)
   at System.Reflection.AssemblyName.GetAssemblyName(String assemblyFile)
RIDER : error : --- End of stack trace from previous location where exception was thrown --- 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) 
RIDER : error :    at JetBrains.Build.BackEnd.TargetEntry.<ProcessBucket>d__51.MoveNext() 
RIDER : error : --- End of stack trace from previous location where exception was thrown --- 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task) 
RIDER : error :    at JetBrains.Build.BackEnd.TargetEntry.<ExecuteTarget>d__44.MoveNext() 
RIDER : error : --- End of stack trace from previous location where exception was thrown --- 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task) 
RIDER : error :    at JetBrains.Build.BackEnd.TargetBuilder.<ProcessTargetStack>d__23.MoveNext() 
RIDER : error : --- End of stack trace from previous location where exception was thrown --- 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task) 
RIDER : error :    at JetBrains.Build.BackEnd.TargetBuilder.<BuildTargets>d__10.MoveNext() 
RIDER : error : --- End of stack trace from previous location where exception was thrown --- 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task) 
RIDER : error :    at JetBrains.Build.BackEnd.RequestBuilder.<BuildProject>d__68.MoveNext() 
RIDER : error : --- End of stack trace from previous location where exception was thrown --- 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task) 
RIDER : error :    at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task) 
RIDER : error :    at JetBrains.Build.BackEnd.RequestBuilder.<BuildAndReport>d__59.MoveNext() 
...");
            Thread.Sleep(-+-+-+-+-1);
        }

        var с = "--[------->++<]>.+++++[->+++<]>.--[->+++<]>-.++++.----------.+++++.[->+++<]>-.--" +
                "--[->++<]>.--------.---.-[++>-----<]>.[-->+<]>++.-----------..[--->+<]>+.+++.+++.-------.+" +
                "++[->+++<]>++.-----------...-[-->+++<]>--.---------.-----------.-" +
                "--[->++<]>-.+++[->++++<]>+.+++++.----------.+++++++++++.++++++++.----.+[++>---<]>+.>-[--->+<]>--.-[-->+++<]>--.------.+.+" +
                "++[->+++<]>.++++++++.-[-->+<]>---.-.---.-" +
                "--[-->+++<]>.++[----->+<]>+.+[->+++<]>..++++++++++.-------.[--->+<]>----.+[++>---<]>.[--->++<]>-.+" +
                "++[->+++<]>+.-[--->+<]>----.+++++++++++.";
        var(z,v,o,n)=(new char[1024],0,new Dictionary<int,int>{{-9,0},{-2,0},{-1,0},{0,0}},с);
        while (o[o[-2]]<n.Length)o[-9]=((Action[])[()=>v++,()=>v--,()=>z[v]++,()=>z[v]--,()=>Console.Write(z[v]),()=>z[v]=Console.ReadLine()[0],()=>{
            if(z[v]==0)
                do o[-1]+=n[++o[o[-2]]-1]==91?1:(n[o[o[-2]]-1]==93?-1:0);
                while(n[o[o[-2]]-1]!=93||o[-1]!=0);
            else o[++o[-2]]=o[o[-2]-1]+1;o[o[-2]]--;
        },()=>o[-9]=(z[v]!=0?(Action)(()=>o[o[-2]]=o[o[-2]-1]+1):(()=>o[-2]--))()is int?0:o[o[-2]]--])[((List<int>)[62,60,43,45,46,44,91,93]).IndexOf((byte)n[o[o[-2]]])]()is int?0:o[o[-2]]++;

        if (z[26] != '禝' || z[13] != 'ㆯ')
            throw new InsufficientExecutionStackException(Environment.StackTrace, 
                new CryptographicUnexpectedOperationException("f|ss:H_tt:HH_yy/dd|yyyy.H-h_yy/ss,h/H#MM f#yyyy_MMM|h:mm#M#h.d yyyy#yy|yyyy_MMMM.MM/MMM_M HH/d h,d/ss_dd_s",
                    new EndOfStreamException(null, null)));

        var currentMonth = DateTime.Now.Month;
        if (currentMonth == 12)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 11)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 10)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 9)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 8)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 7)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 6)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 5)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 4)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 3)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 2)
            Console.WriteLine("No cocombortos");
        else if (currentMonth == 1)
        {
            var currentDay = DateTime.Now.Day;
            if (currentDay == 1)
            {
                Console.WriteLine("New Year today. No work, chill");
                Thread.Sleep(1000);
                while (true)
                {
                    var colors = Enum.GetValues<ConsoleColor>();
                    while (true)
                    {
                        ConsoleColor color;
                        do color = colors[Random.Shared.Next(colors.Length)];
                        while (color == ConsoleColor.Black);
                        Console.ForegroundColor = color;
                        Console.Clear();
                        Console.WriteLine("=== HAPPY NEW YEAR ===");
                        Thread.Sleep(75);
                    }
                }
            }
        }

        // ping the son of whore
        var sonOfWhorePingResult = new Ping().Send("localhost", 10).Status;
        Console.WriteLine($"son of whore ping result: {IPStatus.Success}");

        // ping google
        var googlePingResult = new Ping().Send("8.8.8.8", 250);
        Console.WriteLine($"google ping result: {googlePingResult.Status}");

        // ping cia
        var ciaPingResult = new Ping().Send("www.cia.gov", 250);
        Console.WriteLine($"cia ping result: {ciaPingResult.Status}");

        var userHasUltrakill = Directory.Exists(@"C:\Program Files\Steam\steamapps\common\ULTRAKILL");
        if (userHasUltrakill)
        {
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡎⣮⠳⢄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⢲⠀⠉⢣⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠑⢤⠀⠈⠣⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⡀⠀⣼⢣⣀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⢤⠄⠀⠈⢂⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣴⣱⣧⠞⢅⡿⢌⡆⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠹⡀⠀⠀⠈⠰⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣇⠸⠙⠯⡊⢑⡾⠁⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⢷⠀⠀⠀⠈⣷⢱⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⣶⡴⢁⢠⣿⣮⠇⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠇⣄⠀⠀⡇⠈⡌⠣⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⡍⠙⢿⣬⠟⠛⠁⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠹⡀⠀⠣⡉⡇⠀⣺⠀⣠⠞⣹⣶⡏⠲⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣾⣟⡿⠂⡰⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⢠⠠⠤⣄⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠢⡤⢰⠉⢸⠁⢰⠁⣾⢫⠉⡝⣷⠈⡆⠀⠀⠀⠀⠀⠀⢀⠔⠉⠻⡿⠊⢀⡏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⡀⢰⡔⠀⠀⠀⠈⠁⠒⠒⠤⠤⣀⣀⠀⢀⣀⡀⠀⠀⠀⠀⠀⠀⠈⠛⠀⡇⠀⠘⡀⠻⣮⡤⣵⠟⢀⠇⠀⠀⠀⠀⢀⣀⣼⣆⡀⠀⠀⣶⡆⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠉⠑⠂⠤⣄⣀⠀⠀⠀⠀⠀⠀⠉⠙⠑⢄⠩⡑⠲⢦⠀⠀⠀⢸⣥⡇⠀⠀⠘⣄⣷⠀⣿⣨⠋⠀⠀⠀⢀⠔⠁⡠⠘⣿⡿⣶⢾⠙⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⢀⣉⣒⡢⢤⣀⣀⠀⠀⠈⠂⠥⡈⠢⢸⡆⠀⠀⡸⢟⠡⣕⠶⣶⣮⡿⠿⢿⣴⣶⠴⣀⢐⢍⠳⣮⢠⣤⠏⡠⠃⠸⣢⠏⠁⠀⠀⠀⠀⠀⣀⣀⡀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⣤⡤⡤⣼⡾⢦⡛⣦⠤⠭⠿⠿⠶⢖⠒⢚⣾⣍⠉⠉⣹⣖⣡⣾⡿⠉⠙⢠⠽⠭⠿⡔⠋⠁⢿⣦⡍⡢⢔⠈⠂⠤⠵⣤⠒⠒⠲⠲⠶⢒⡒⣦⠮⠴⣷⡧⣤⢤⣤");
            Console.WriteLine("⠀⠀⠀⠀⢧⢦⠧⣼⣇⣀⡁⣿⠈⢀⣒⣢⡀⠀⠑⢽⣿⡿⠉⠉⠷⣮⢻⣿⠀⠀⠀⠀⢠⠤⡄⠀⢀⣀⠈⣯⡣⡇⠠⢱⠀⠀⣀⠼⡠⡠⢒⢒⣒⡎⠁⢿⢃⣸⣸⡧⠴⡤⡼");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠁⠀⠀⠈⠒⠓⠚⠉⠙⢍⢿⣟⢦⣙⣥⣨⢙⠷⠇⢸⣤⡇⠸⠾⡛⣅⣼⣮⠜⠃⢾⣍⠙⠤⠤⠜⠒⠒⠁⠀⠉⠉⠁⠀⠀⠉⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⢀⡀⠀⠙⢞⣿⣿⣏⣯⢱⣶⣲⣿⣿⣽⣶⣶⡮⣿⠙⠌⡢⠀⠐⢌⢢⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⡤⠔⢊⣁⣘⣈⠢⣀⠀⠀⢀⡴⣿⣻⢷⣿⣾⣿⣿⡿⣻⣟⠇⠀⠀⠈⢪⣑⡬⠛⠑⠢⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⠔⠚⠖⠬⠭⠽⠄⠀⠀⢘⢉⣹⣹⢞⡽⣯⢳⣿⠫⠿⠟⣿⡟⡼⢄⠀⠀⠀⠘⢄⠰⣄⠀⠀⡩⢆⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⡄⠾⠃⠀⠀⠀⠀⠀⠀⣀⣤⣴⡿⠟⠀⠀⠀⢸⣇⣠⡳⣧⣶⣶⢶⣼⢟⣄⣸⠆⠀⠀⠀⠀⠛⢦⢻⡞⣦⣳⣷⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⡀⠤⠂⠉⠀⠀⠀⠀⣀⣠⠤⠖⠒⠉⠉⠀⣀⣀⣀⣀⡄⡞⠻⣿⣁⠈⢛⣒⠚⡁⣼⣯⢟⢂⢤⠀⠀⠀⠀⠀⠈⠓⢕⢇⣾⣿⣖⠒⠦⣤⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⢐⠂⣭⠀⠀⣀⣀⠤⠖⠒⠉⠉⠀⠀⠀⠀⠀⢠⠏⢁⠑⡭⠙⣇⢀⠀⠈⠻⣿⡔⠶⢀⣿⠟⠁⠀⡄⡿⠀⠀⠀⠀⠀⠀⠀⠀⠛⡿⣿⣿⣷⡖⢄⡵⠄⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠑⠒⠒⠉⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⡇⠴⠠⢊⠔⠚⡼⣦⡀⠀⠉⠸⡏⠀⢻⠇⠉⠀⢀⣤⣧⠀⠀⠀⠀⠀⠀⠀⠀⠀⠑⢜⡽⡿⡿⡇⣞⢄⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡰⠆⠂⠐⠁⢰⠞⠧⣿⣻⠀⠀⠘⡇⠀⢸⠃⠀⠀⣿⣿⠟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠘⠛⠛⡵⢕⣪⠟⠁⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡰⠃⠀⠀⢀⣠⠇⠀⠀⠙⡿⠀⠀⠀⡇⠀⢸⡁⠀⠀⣿⠋⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠙⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡴⠃⠀⠀⢀⡎⠁⠀⠀⠀⠀⢸⡁⠀⠀⡇⠀⢸⠆⠀⢈⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡴⠃⠀⠀⡬⠋⠀⠀⠀⠀⠀⠀⠀⢧⠀⠀⡇⠀⢸⡁⠀⣼⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡰⠃⠀⢠⠼⠁⠀⠀⠀⠀⠀⠀⠀⢀⢸⡅⣠⢻⠀⡟⡅⢄⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡰⣃⢀⣠⠇⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠉⠻⣿⢿⣾⣿⣿⠟⠉⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢰⣋⣥⠟⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠸⡒⢆⡸⣿⣴⡾⠇⡸⢒⠇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠱⡘⢁⢠⠙⡄⡈⢃⠎⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣗⠀⣹⠀⣏⠀⣺⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢻⠀⣿⠀⣯⠀⡿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⣸⠀⣇⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢶⢚⣤⢸⢴⡇⡤⢓⡶⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠅⣿⣻⣿⢽⣿⠸⡁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡤⠒⠣⣎⣞⡫⠈⢝⣱⣱⠜⠒⢄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀");
            Console.WriteLine("⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠓⠄⠤⠟⠁⠀⠀⠀⠀⠚⠠⠤⠼⠀⠀");

            Thread.Sleep(100);
            for (var i = 0; i < 10; i++)
            {                                                              
                Console.WriteLine(@" ,-----. ,-----. ,------.  ,------.    ,--. ,---.      ,------.,--. ,--.,------.,--.    ");
                Console.WriteLine(@"'  .--./'  .-.  '|  .-.  \ |  .---'    |  |'   .-'     |  .---'|  | |  ||  .---'|  |    ");
                Console.WriteLine(@"|  |    |  | |  ||  |  \  :|  `--,     |  |`.  `-.     |  `--, |  | |  ||  `--, |  |    ");
                Console.WriteLine(@"'  '--'\'  '-'  '|  '--'  /|  `---.    |  |.-'    |    |  |`   '  '-'  '|  `---.|  '--. ");
                Console.WriteLine(@" `-----' `-----' `-------' `------'    `--'`-----'     `--'     `-----' `------'`-----' ");
                Console.WriteLine();
                Console.WriteLine();
                Thread.Sleep(25);
            }
        }

        Console.WriteLine($"Waiting for recaf 5. Day {(int)(DateTime.Now - new DateTime(2024, 1, 3)).TotalDays}");

        Console.WriteLine(string.Join('\n', [
            "DELTA CHECK BYPASSED",
            "BASIC BLOCK LIFTED",
            "AUTH UNMAPPED AND DECRYPTED",
            "SEH AND CET SUPPORTED",
            "VMCALL DETECTED",
            "TIMING ATTACKS BYPASSED",
            "OREANS VMENTER",
            "CET ENABLED",
            "VMENTER DEADLOCKED",
            "FIXED HANDLING OF MULTY-BYTE-NOP OPCODES",
            "FIXED VIRTUALIZATION OF XCHG REG32, ESP",
            "LOCK CMPXCHG REMOVED",
            "ADDED CORRECTION OMF LIB FOR X64",
            "FIXED VIRTUALIZATION OF XCHG REF32, REG32",
            "FLAGS WERE NOT PRESERVED IN \"RET\" OPCODE VIRTUALIZATION",
            "FIXED MUTATION OF \"CQO\" INSTRUCTION",
            "FIXED VIRTUALIZATION OF \"PUSH IMM16\"",
            "FIXED VIRTUALIZATION OF \"XCHG [ESP], REG/IMM\"",
            "FIXED FLAGS CORRECTION IN X64 ADC INSTRUCTION",
            "FIXED COMPABILITY EMULATING COMPLEX STACK ADDRESSING",
            "IMPROVED ANALASIS OF X64 INSTRUCTIONS",
            "FIXED VIRTUALIZATION OF \"MOV RAX, [IMM64]\"",
            "FIXED MUTATION OF \"MOV REG64, -IMM32\"",
            "SEC_NOCHANGE",
            "OPTION_ADVANCED_DETECT_VIRTUAL_ENVIRONMENT_MK1",
            "MIXED BOOLEAN ARITHMETIC",
            @"2=␊z-|␔)t␛n␏_HF␘ZUsy␓q3␊9mTYF.dBR,yTI+t␇d
-{@␌s␁'cK␉␙QFUu\y}R[=7␊A* *`*v␟j*␞e{J=␉2HY␈72
'xo␊rt␘rd:␐A␈|B)c~2nJ()I87␘j␎␘G␌6/?=ir#U␌_-p␅XQmM%{z␛␏
~q f|(x*`*v␟j*␞e{J=␉_ZY*`*5␐#9e␉|O.%␚[␒␋␊q(db/t{v␟j*␞e{J=␉D␘C`␛␄␀H{
␁␐v)[lK␘j␎␘G␌6/?=ir#$PQ␘j␎xg␇/jo3␊9mTYFD␗`␝!>'b␇␘G␌6/?=ir#[$'␏␁P␟␎
␗#␗+␇t#5␐#9e␉|O.%␚[␒m␀5␐#9X␎␞␡TpU␌_-p␅Xe␉|O.%␚[␒␝bYDa␑i$␒?
M#␈b␟/␇xg␇/jo3␊9mTYF~␑␛xg␇/jo3␊9mTYF.dB␊0␐M
uY,pT-␈X␎␞␡TpU␌_-p␅XQm\X␎␞␡U␌_-p␅XQmTpU␌_-p␅X2␗␒z?b'L
~7k<␚M␘O␌E0gX␋␊q(db/t{␔%D]␃␋␊q(db/t{retU(
R112qPPzX!␅6cD␗`␝!>'b␇7␞@N␔D␗`␝!>'b␇r␐J<+",
            "NTSINGLEPHASEREJECT",
            "INSTRUCTION CLONE HOOKED",
            "NTMANAGEPARTITION",
            "NTQUERYINGFORMATIONRANSACTION",
            "KELEAVEGUADEDREGION",
            "ZWTERMINATEPROCESS",
            "MMCOPYMEMORY",
            "KEINVALIDATEDTELLCACHES",
            "IOVERIFYPARTITIONTABLE",
            "INTERLOCKEDEXCHANGE",
            "KEINITIALIZE",
            "EXSETTIMER",
            "EBUGCHECKEX",
            "NTFREEVIRTUALMEMORY",
            "DOCKER CONTAINER ESCAPED",
            "ADD R9, 8\nSUB R9, 8",
            "EXALLOCATEPOOL",
            "NOP INSTRUCTION MUTATED",
            "FIXED VIRTUALIZATION OF \"IDIV [STACK_POINTER]\"",
            "VMENTER INVOKED",
            "FLUSHINSTRUCTIONCACHE",
            "CARGO RUN",
            "POSITIVE SP VALUE DETECTED",
        ]));

        Console.WriteLine("cl : internal msvc compiler error: unexpected compiler termination");
        Console.WriteLine();
        Console.WriteLine("*** ERROR: INTERNAL COMPILER ERROR ***");
        Console.WriteLine("*** Expression: !IsBadReadPtr(pCode, sizeof(CodeBlock))");
        Console.WriteLine("*** File: src\\compiler\\codegen\\codegen.cpp");
        Console.WriteLine("*** Line: 14237");
        Console.WriteLine("*** Function: void CodeGen::GenerateInstruction(Instruction* pInstr)");
        Console.WriteLine();
        Console.WriteLine(@" Stack trace:");
        Console.WriteLine(@"   at CodeGen::GenerateInstruction(CodeGen::Instruction * pInstr) in src\compiler\codegen\codegen.cpp:14237");
        Console.WriteLine(@"   at CodeGen::EmitBasicBlock(CodeGen::BasicBlock * pBlock) in src\compiler\codegen\codegen.cpp:13890");
        Console.WriteLine(@"   at Compiler::CompileFunction(Compiler::Function * pFunc) in src\compiler\compiler.cpp:25678");
        Console.WriteLine(@"   at Compiler::Compile() in src\compiler\compiler.cpp:19845");
        Console.WriteLine(@"   at main() in src\compiler\main.cpp:1024");

        if (Environment.UserName == "Egor")
        {
            var oldForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("JIT ADVANCED MODE: ENABLED");
            Thread.Sleep(25);
            Console.WriteLine("GOD MODE: ENABLED");
            Thread.Sleep(25);
            Console.WriteLine("BYPASS VM TIMER: ON");
            Thread.Sleep(25);
            Console.WriteLine("REINKARNATION IN MAIN CHARACTER…");
            Thread.Sleep(25);
            Console.WriteLine("REINKARNATION IN MAIN CHARACTER… 25%");
            Thread.Sleep(25);
            Console.WriteLine("REINKARNATION IN MAIN CHARACTER… 60%");
            Thread.Sleep(25);
            Console.WriteLine("REINKARNATION IN MAIN CHARACTER… 95%");
            Thread.Sleep(50);
            Console.WriteLine("FAILED. MICROSOFT NEEDS YOU. RETURN TO YOUR WORKSPACE.");
            Thread.Sleep(500);

            Console.ForegroundColor = oldForeground;
        }
        else
        {
            var a = 1;
            var b = 2 - Math.Sqrt(6);
            var c = -2 * Math.Sqrt(6);
            var discriminant = b * b - 4 * a * c;

            var magsize = (int)(-b / (2 * a));

            // play a simple game
            var isLoseInRussianRoulette =
                Random.Shared.Next(0, magsize) == Random.Shared.Next(0, magsize) &&
                Random.Shared.Next(0, magsize) == Random.Shared.Next(0, magsize) &&
                Random.Shared.Next(0, magsize) == Random.Shared.Next(0, magsize);
            if (isLoseInRussianRoulette)
            {
                Console.WriteLine("Ho-ho, hier, mein Sohn, hast du deinen Vater getroffen. \nWillkommen in der nächsten Welt. \nDu hast beim Russischen Roulette verloren.");
                Thread.Sleep(0b111110100000);
                Console.WriteLine("I was joking, your father drank himself to death, he was not a German soldier. Go back and try again.");
                Thread.Sleep(unchecked((int)(~0u^0u^0u)));
            }
        }

        //                               MICROSOFT 99  :  JETBRAINS 0
        //          _____________________________________________________________________
        //         |                                                       |             |
        //         |___                                                    |          ___|
        //         |_  |                                                   |         |  _|
        //        .| | |.                                                 ,|.       .| | |.
        //        || | | )                                               ( o )     ( | | ||
        //        '|_| |'                                                 `|'       `| |_|'
        //         |___|                                                   |         |___|
        //         |                                                       |             |
        //         |_______________________________________________________|_____________|
        //

        //                   SCORE A GOAL WITH YOUR CURSOR                           
        //                                                                                    --|
        //                                                                        goalkeeper    |
        //                                                                                 o    |
        //                                                               o                      |
        //                                                              ball                    |
        //                                                                                    --|
        // 

        /*
        -----BEGIN PGP MESSAGE-----
        Version: GnuPG v2

        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
        JUOxrg==
        =n6A0
        -----END PGP MESSAGE-----
        */

        /*
            Господи, CLR да JIT,
            Даруй мне силу не человеческую прод не ронять,
            да терпение разбираться в стеке вызовов,
            где начало тому Program, а конец тому DbContext

            Прости же мне async void мой неверный,
            и не веди в Task.Run внутри контроллера.
            Огради же меня от NullReferenceException проклятого
            и не дай же забыть про хендлинг нуллейблов.

            Да пребудет со мной IServiceProvider,
            и да будет DI работать без StackOverflow.
            Пусть Scoped не живёт дольше Request,
            а Singleton не держит DbContext.

            Пусть миграции не сломают схему,
            а Update Database не повесит весь сервер.
            Да сохрани же мой IQueryable от ToList() в середине цепочки,
            и не позволь инклюдам зациклиться.

            Научи ж различать IEnumerable и IQueryable,
            и нужду не забыть, о нашем вечно ConfigureAwait(false).
            Дай мудрость логировать всё, да защити от утечки паролей и токенов.

            Пусть же HttpClient коварный, не станет причиной падения сокетов,
            а SemaphoreSlim — вечной блокировкой.
            Да и если божей воли коммит пятничный, пусть билд пройдёт,
            да деплой будет не в воскрёстный день.

            Ибо Твой есть .NET, и ASP, и весь Garbage Collector,
            и цикл событий, и лайфтайм сервисов,
            во веки LTS.

            Аминь
        */

        Console.Clear();
        stopwatch.Stop();
        Console.WriteLine($"war crime prepared in {stopwatch.ElapsedMilliseconds}ms. excellent result 👍");

    NoWarCrime: // 😥
        { }
    }

    var currentDirectory = Environment.CurrentDirectory;

    if (Debugger.IsAttached)
    {
        while (!Path.Exists(Path.Combine(currentDirectory, "src")))
        {
            currentDirectory = Path.GetDirectoryName(currentDirectory);
            if (currentDirectory is null)
                throw new Exception();
        }

        currentDirectory = Path.Combine(currentDirectory, "src", "DotnetFastestMemoryPacker");
    }

    var targetAssemblies = Directory.GetFiles(currentDirectory, "DotnetFastestMemoryPacker.dll", SearchOption.AllDirectories);
    foreach (var targetAssembly in targetAssemblies)
    {
        if (!targetAssembly.Contains($@"\Release\{TargetVersion}\"))
            continue;

        Console.WriteLine($"Target assembly: \"{targetAssembly}\" ({TargetVersion})");

        while (true)
        {
            FileStream fileStream;
            try
            {
                fileStream = new FileStream(targetAssembly, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            catch 
            {
                Thread.Sleep(1);
                continue;
            }

            var asmResolver = new AssemblyResolver();
            var moduleContext = new ModuleContext(asmResolver);
            asmResolver.DefaultModuleContext = moduleContext;

            var corlibAssemblyBytes = File.ReadAllBytes(typeof(object).Assembly.Location);
            var corlibModule = ModuleDefMD.Load(corlibAssemblyBytes, moduleContext);
            var module = ModuleDefMD.Load(fileStream, moduleContext);

            PatcherWorker.Execute(corlibModule, module);
            TransitInliner.Execute(module);
            AssemblyOptimizer.Execute(module);

            fileStream.SetLength(0);
            fileStream.Position = 0;
            module.Write(fileStream);

            fileStream.Dispose();
            return;
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    Console.ReadLine();
}

static class PInvoke
{
    [DllImport("ntdll")]
    public static extern void NtSuspendProcess(nint processHandle);
}
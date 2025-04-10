using System.Text;

string caminhoArquivo = "./hexText.txt";

string ConverterHexParaBinario(char caractere)
{
    return caractere switch
    {
        '0' => "0000",
        '1' => "0001",
        '2' => "0010",
        '3' => "0011",
        '4' => "0100",
        '5' => "0101",
        '6' => "0110",
        '7' => "0111",
        '8' => "1000",
        '9' => "1001",
        'A' or 'a' => "1010",
        'B' or 'b' => "1011",
        'C' or 'c' => "1100",
        'D' or 'd' => "1101",
        'E' or 'e' => "1110",
        'F' or 'f' => "1111",
        _ => throw new ArgumentException($"Caractere inválido: {caractere}")
    };
}

string ObterTipoInstrucao(string opcode)
{
    return opcode switch
    {
        "0110011" => "R-Type",
        "0010011" => "I-Type",
        "1110011" => "I-Type",
        "0000011" => "I-Type",
        "0001111" => "I-Type",
        "0100011" => "S-Type",
        "1100011" => "B-Type",
        "1101111" => "J-Type",
        "1100111" => "I-Type",
        "0110111" => "U-Type",
        "0010111" => "U-Type",
        _ => throw new ArgumentException($"Não foi possível identificar a instrução")
    };
}

void IdentificarInstrucoesBinarias(string caminhoArquivo)
{
    if (!File.Exists(caminhoArquivo))
    {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    // Dicionário para contar as instruções por tipo
    var contadorInstrucoes = new Dictionary<string, int>
    {
        { "R-Type", 0 },
        { "I-Type", 0 },
        { "S-Type", 0 },
        { "B-Type", 0 },
        { "J-Type", 0 },
        { "U-Type", 0 }
    };

    foreach (var linha in File.ReadLines(caminhoArquivo))
    {
        var resultadoBinario = new StringBuilder();

        foreach (var caractere in linha)
        {
            var binario = ConverterHexParaBinario(char.ToUpper(caractere));
            if (binario != null)
            {
                resultadoBinario.Append(binario);
            }
            else
            {
                Console.WriteLine($"Caractere inválido encontrado: {caractere}");
                return;
            }
        }

        var binarioCompleto = resultadoBinario.ToString();
        var opcode = binarioCompleto.Length >= 7 ? binarioCompleto.Substring(binarioCompleto.Length - 7) : null;

        if (opcode != null)
        {
            var tipoInstrucao = ObterTipoInstrucao(opcode);
            if (tipoInstrucao != null)
            {
                Console.WriteLine($"Hexadecimal: {linha} -> Binário: {binarioCompleto} -> Tipo de Instrução: {tipoInstrucao}");
                // Incrementa o contador para o tipo de instrução
                if (contadorInstrucoes.ContainsKey(tipoInstrucao))
                {
                    contadorInstrucoes[tipoInstrucao]++;
                }
            }
            else
            {
                Console.WriteLine($"Hexadecimal: {linha} -> Binário: {binarioCompleto} -> Tipo de Instrução: Opcode inválido ou não encontrado.");
            }
        }
        else
        {
            Console.WriteLine($"Hexadecimal: {linha} -> Binário: {binarioCompleto} -> Tipo de Instrução: Opcode inválido ou não encontrado.");
        }
    }

    Console.WriteLine("\nResumo das instruções:");
    Console.WriteLine($"R-Type: {contadorInstrucoes["R-Type"]}  I-Type: {contadorInstrucoes["I-Type"]}  S-Type: {contadorInstrucoes["S-Type"]}");
    Console.WriteLine($"B-Type: {contadorInstrucoes["B-Type"]}  J-Type: {contadorInstrucoes["J-Type"]}  U-Type: {contadorInstrucoes["U-Type"]}");
}

IdentificarInstrucoesBinarias(caminhoArquivo);
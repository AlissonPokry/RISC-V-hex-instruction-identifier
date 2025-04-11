using System.Text;

// Caminho do arquivo que contém as instruções em hexadecimal
string caminhoArquivo = "./hexText.txt";

// Função para converter um caractere hexadecimal em sua representação binária
string ConverterHexParaBinario(char caractere)
{
    // Utiliza um switch expression para mapear cada caractere hexadecimal para seu equivalente binário
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
        // Lança uma exceção se o caractere não for válido
        _ => throw new ArgumentException($"Caractere inválido: {caractere}")
    };
}

// Função para identificar o tipo de instrução com base no opcode
string ObterTipoInstrucao(string opcode)
{
    // Utiliza um switch expression para mapear cada opcode para seu tipo de instrução
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
        // Lança uma exceção se o opcode não for reconhecido
        _ => throw new ArgumentException($"Não foi possível identificar a instrução")
    };
}

// Função para identificar e contar instruções binárias a partir de um arquivo
void IdentificarInstrucoesBinarias(string caminhoArquivo)
{
    // Verifica se o arquivo existe
    if (!File.Exists(caminhoArquivo))
    {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    // Dicionário para contar a quantidade de instruções por tipo
    var contadorInstrucoes = new Dictionary<string, int>
    {
        { "R-Type", 0 },
        { "I-Type", 0 },
        { "S-Type", 0 },
        { "B-Type", 0 },
        { "J-Type", 0 },
        { "U-Type", 0 }
    };

    // Lê o arquivo linha por linha
    foreach (var linha in File.ReadLines(caminhoArquivo))
    {
        var resultadoBinario = new StringBuilder();

        // Converte cada caractere hexadecimal da linha para binário
        foreach (var caractere in linha)
        {
            var binario = ConverterHexParaBinario(char.ToUpper(caractere));
            resultadoBinario.Append(binario);
        }

        // Obtém o binário completo da linha
        var binarioCompleto = resultadoBinario.ToString();
        // Extrai os últimos 7 bits para identificar o opcode
        var opcode = binarioCompleto.Length >= 7 ? binarioCompleto.Substring(binarioCompleto.Length - 7) : null;

        if (opcode != null)
        {
            // Identifica o tipo de instrução com base no opcode
            var tipoInstrucao = ObterTipoInstrucao(opcode);
            if (tipoInstrucao != null)
            {
                // Exibe o resultado da conversão e o tipo de instrução
                Console.WriteLine($"Hexadecimal: {linha} -> Binário: {binarioCompleto} -> Tipo de Instrução: {tipoInstrucao}");
                // Incrementa o contador para o tipo de instrução
                if (contadorInstrucoes.ContainsKey(tipoInstrucao))
                {
                    contadorInstrucoes[tipoInstrucao]++;
                }
            }
            else
            {
                // Mensagem de erro caso o opcode não seja válido
                Console.WriteLine($"Hexadecimal: {linha} -> Binário: {binarioCompleto} -> Tipo de Instrução: Opcode inválido ou não encontrado.");
            }
        }
        else
        {
            // Mensagem de erro caso o opcode não seja encontrado
            Console.WriteLine($"Hexadecimal: {linha} -> Binário: {binarioCompleto} -> Tipo de Instrução: Opcode inválido ou não encontrado.");
        }
    }

    // Exibe o resumo das instruções identificadas
    Console.WriteLine("\nResumo das instruções:");
    Console.WriteLine($"R-Type: {contadorInstrucoes["R-Type"]}  I-Type: {contadorInstrucoes["I-Type"]}  S-Type: {contadorInstrucoes["S-Type"]}");
    Console.WriteLine($"B-Type: {contadorInstrucoes["B-Type"]}  J-Type: {contadorInstrucoes["J-Type"]}  U-Type: {contadorInstrucoes["U-Type"]}");
}

// Chama a função principal para processar o arquivo
IdentificarInstrucoesBinarias(caminhoArquivo);
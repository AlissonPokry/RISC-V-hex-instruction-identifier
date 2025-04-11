const string MENSAGEM_ERRO = "Opcode inválido ou não encontrado.";

// Caminho do arquivo que contém as instruções em hexadecimal
string caminhoArquivo = "./hexText.txt";

string ConverterHexParaBinario(char caractere) {
    return caractere switch {
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
        'A' => "1010",
        'B' => "1011",
        'C' => "1100",
        'D' => "1101",
        'E' => "1110",
        'F' => "1111",
        // Lança uma exceção se o caractere não for válido
        _ => throw new ArgumentException($"Caractere inválido: {caractere}")
    };
}

string ObterTipoInstrucao(string opcode) {
    return opcode switch {
        "0110011" => "R-Type",
        "0010011" => "I-Type",
        "1110011" => "I-Type",
        "0000011" => "I-Type",
        "0001111" => "I-Type",
        "0100011" => "S-Type",
        "1100011" => "B-Type",
        "1101111" => "J-Type",
        "0110111" => "U-Type",
        "0010111" => "U-Type",
        // Lança uma exceção se o opcode não for reconhecido
        _ => throw new ArgumentException($"Não foi possível identificar a instrução")
    };
}

// Função para exibir o resultado da conversão e identificação
void ExibirResultado(string hex, string binario, string tipo) {
    Console.WriteLine($"Hexadecimal: {hex} -> Binário: {binario} -> Tipo de Instrução: {tipo}");
}

// Função para exibir o resumo das instruções identificadas
void ExibirResumo(Dictionary<string, int> contador) {
    Console.WriteLine("\nResumo das instruções:");
    Console.WriteLine($"R-Type: {contador["R-Type"]}  I-Type: {contador["I-Type"]}  S-Type: {contador["S-Type"]}");
    Console.WriteLine($"B-Type: {contador["B-Type"]}  J-Type: {contador["J-Type"]}  U-Type: {contador["U-Type"]}");
}

void IdentificarInstrucoesBinarias(string caminhoArquivo) {
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
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        // Converte cada caractere hexadecimal da linha para binário
        var binarioCompleto = string.Concat(linha.Select(caractere => ConverterHexParaBinario(char.ToUpper(caractere))));

        // Verifica se o binário tem pelo menos 7 bits, caso contrário, exibe mensagem de erro
        if (binarioCompleto.Length < 7) {
            ExibirResultado(linha, binarioCompleto, MENSAGEM_ERRO);
            continue;
        }

        // Atribui os últimos 7 bits do binário a uma variável de opcode
        var opcode = binarioCompleto.Substring(binarioCompleto.Length - 7);

        try {
            var tipoInstrucao = ObterTipoInstrucao(opcode);
            
            ExibirResultado(linha, binarioCompleto, tipoInstrucao);
            
            contadorInstrucoes[tipoInstrucao]++;
        }
        catch (ArgumentException) {
            // Se o opcode não for reconhecido, exibe mensagem de erro para a instrução atual
            ExibirResultado(linha, binarioCompleto, MENSAGEM_ERRO);
        }
    }

    // Exibe o resumo das instruções identificadas
    ExibirResumo(contadorInstrucoes);
}


// Chama a função principal para processar o arquivo
IdentificarInstrucoesBinarias(caminhoArquivo);
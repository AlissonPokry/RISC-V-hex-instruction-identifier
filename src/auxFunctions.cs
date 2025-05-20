using System.Text;

public class auxFunctions
{
    // Mensagem padrão para erro de opcode
    const string MENSAGEM_ERRO = "Opcode inválido ou não encontrado.";

    // Converte um caractere hexadecimal em sua representação binária de 4 bits
    public string ConverterHexParaBinario(char caractere)
    {
        // Utiliza switch expression para mapear cada caractere
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

    // Retorna o tipo da instrução a partir do opcode
    public string ObterTipoInstrucao(string opcode)
    {
        // Mapeia opcodes para tipos de instrução RISC-V
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
            "0110111" => "U-Type",
            "0010111" => "U-Type",
            // Lança uma exceção se o opcode não for reconhecido
            _ => throw new ArgumentException($"Não foi possível identificar a instrução")
        };
    }

    // Identifica a instrução assembly a partir dos campos opcode, funct3 e funct7
    public string IdentificarInstrucaoAssembly(string opcode, string funct3, string funct7)
    {
        // Utiliza pattern matching para identificar instruções
        return (opcode, funct3, funct7) switch
        {
            // Instruções tipo R
            ("0110011", "000", "0000000") => "add",
            ("0110011", "000", "0100000") => "sub",
            ("0110011", "001", "0000000") => "sll",
            ("0110011", "010", "0000000") => "slt",
            ("0110011", "011", "0000000") => "sltu",
            ("0110011", "100", "0000000") => "xor",
            ("0110011", "101", "0000000") => "srl",
            ("0110011", "101", "0100000") => "sra",
            ("0110011", "110", "0000000") => "or",
            ("0110011", "111", "0000000") => "and",

            // Instruções tipo I
            ("0010011", "000", _) => "addi",
            ("0010011", "001", _) => "slli",
            ("0010011", "010", _) => "slti",
            ("0010011", "011", _) => "sltiu",
            ("0010011", "100", _) => "xori",
            ("0010011", "101", "0000000") => "srli",
            ("0010011", "101", "0100000") => "srai",
            ("0010011", "110", _) => "ori",
            ("0010011", "111", _) => "andi",

            // Instruções de Load
            ("0000011", "000", _) => "lb",
            ("0000011", "001", _) => "lh",
            ("0000011", "010", _) => "lw",
            ("0000011", "100", _) => "lbu",
            ("0000011", "101", _) => "lhu",

            // Instruções de Store
            ("0100011", "000", _) => "sb",
            ("0100011", "001", _) => "sh",
            ("0100011", "010", _) => "sw",

            // Instruções de Branch
            ("1100011", "000", _) => "beq",
            ("1100011", "001", _) => "bne",
            ("1100011", "100", _) => "blt",
            ("1100011", "101", _) => "bge",
            ("1100011", "110", _) => "bltu",
            ("1100011", "111", _) => "bgeu",

            // Instruções tipo U
            ("0110111", _, _) => "lui",
            ("0010111", _, _) => "auipc",
            
            // Instruções tipo J
            ("1101111", _, _) => "jal",

            _ => "Instrução não identificada"
        };
    }

    // Separa os campos de uma instrução binária de 32 bits
    public (string opcode, string rd, string funct3, string rs1, string rs2, string funct7) SepararCamposInstrucao(string binario)
    {
        if (binario.Length != 32)
        {
            throw new ArgumentException("A instrução deve ter 32 bits");
        }

        // Extrai os campos conforme o formato RISC-V
        string opcode = binario.Substring(25, 7);   // bits 6-0
        string rd = binario.Substring(20, 5);       // bits 11-7
        string funct3 = binario.Substring(17, 3);   // bits 14-12
        string rs1 = binario.Substring(12, 5);      // bits 19-15
        string rs2 = binario.Substring(7, 5);       // bits 24-20
        string funct7 = binario.Substring(0, 7);    // bits 31-25

        return (opcode, rd, funct3, rs1, rs2, funct7);
    }

    // Exibe o resultado da conversão e identificação da instrução
    public void ExibirResultado(string hex, string binario, string tipo, string assembly = "")
    {
        Console.WriteLine($"Hexadecimal: {hex} -> Binário: {binario} -> Tipo: {tipo} -> Assembly: {assembly}");
    }

    // Exibe o resumo das instruções identificadas em um bloco
    public void ExibirResumo(Dictionary<string, int> contador)
    {
        Console.WriteLine("\nResumo das instruções:");
        Console.WriteLine($"R-Type: {contador["R-Type"]}  I-Type: {contador["I-Type"]}  S-Type: {contador["S-Type"]}");
        Console.WriteLine($"B-Type: {contador["B-Type"]}  J-Type: {contador["J-Type"]}  U-Type: {contador["U-Type"]}");
    }

    // Lê um arquivo de instruções hexadecimais, identifica e exibe informações sobre cada instrução
    public void IdentificarInstrucoesBinarias(string caminhoArquivo)
    {
        if (!File.Exists(caminhoArquivo))
        {
            Console.WriteLine("Arquivo não encontrado.");
            return;
        }

        // Inicializa o contador de tipos de instrução
        var contadorInstrucoes = new Dictionary<string, int> {
        { "R-Type", 0 },
        { "I-Type", 0 },
        { "S-Type", 0 },
        { "B-Type", 0 },
        { "J-Type", 0 },
        { "U-Type", 0 }
    };

        int blocoAtual = 1;
        bool inicioBloco = true;

        // Lê todas as linhas do arquivo
        var linhas = File.ReadLines(caminhoArquivo).ToList();

        foreach (var linha in linhas)
        {
            // Se encontrar uma linha em branco, prepara para o próximo bloco
            if (string.IsNullOrWhiteSpace(linha))
            {
                if (!inicioBloco)
                {
                    ExibirResumo(contadorInstrucoes);
                    contadorInstrucoes = contadorInstrucoes.ToDictionary(x => x.Key, x => 0);
                    blocoAtual++;
                    inicioBloco = true;
                }
                continue;
            }

            if (inicioBloco)
            {
                Console.WriteLine($"\n=== Bloco {blocoAtual} ===");
                inicioBloco = false;
            }

            // Converte cada caractere hexadecimal da linha para binário
            var binarioCompleto = string.Concat(linha.Select(caractere => ConverterHexParaBinario(char.ToUpper(caractere))));

            if (binarioCompleto.Length != 32)
            {
                ExibirResultado(linha, binarioCompleto, MENSAGEM_ERRO);
                continue;
            }

            try
            {
                var campos = SepararCamposInstrucao(binarioCompleto);
                var tipoInstrucao = ObterTipoInstrucao(campos.opcode);
                var instrucaoAssembly = IdentificarInstrucaoAssembly(campos.opcode, campos.funct3, campos.funct7);

                ExibirResultado(linha, binarioCompleto, tipoInstrucao, instrucaoAssembly);

                contadorInstrucoes[tipoInstrucao]++;
            }
            catch (ArgumentException)
            {
                ExibirResultado(linha, binarioCompleto, MENSAGEM_ERRO);
            }
        }

        // Exibe o resumo do último bloco se houver instruções
        if (!inicioBloco)
        {
            ExibirResumo(contadorInstrucoes);
        }
    }

    // Escreve o conteúdo em um arquivo, criando a pasta se necessário
    public void EscreverArquivo(string conteudo, string nomeArquivo, string pasta = "outputs")
    {
        try
        {
            var diretorio = Path.Combine(".", pasta);
            Directory.CreateDirectory(diretorio);
            var caminhoCompleto = Path.Combine(diretorio, nomeArquivo);

            // Limpa o arquivo se ele já existir
            if (File.Exists(caminhoCompleto))
            {
                File.WriteAllText(caminhoCompleto, string.Empty);
            }

            File.WriteAllText(caminhoCompleto, conteudo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao escrever no arquivo: {ex.Message}");
        }
    }

    // Separa as linhas do arquivo em blocos (separados por linhas em branco)
    public List<List<string>> SepararEmBlocos(string caminhoArquivo)
    {
        if (!File.Exists(caminhoArquivo))
        {
            Console.WriteLine("Arquivo não encontrado.");
            return new List<List<string>>();
        }

        var blocos = new List<List<string>>();
        var blocoAtual = new List<string>();

        foreach (var linha in File.ReadLines(caminhoArquivo))
        {
            if (string.IsNullOrWhiteSpace(linha))
            {
                if (blocoAtual.Count > 0)
                {
                    blocos.Add(new List<string>(blocoAtual));
                    blocoAtual.Clear();
                }
                continue;
            }
            blocoAtual.Add(linha);
        }

        if (blocoAtual.Count > 0)
        {
            blocos.Add(blocoAtual);
        }

        return blocos;
    }

    // Exibe a sequência original de instruções de um bloco
    public void ExibirSequenciaOriginal(List<string> bloco, StringBuilder outputBuilder)
    {
        outputBuilder.AppendLine("Sequência original:");
        for (int j = 0; j < bloco.Count; j++)
        {
            var (_, assembly, _, _, _) = AnalisarInstrucao(bloco[j]);
            outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
        }
        outputBuilder.AppendLine();
    }

    // Exibe o sobrecusto (percentual de NOPs) para determinada técnica
    public void ExibirSobrecusto(string titulo, int totalInstrucoes, int totalNops, int? instrucoesReordenadas = null)
    {
        Console.WriteLine($"\n=== Sobrecusto {titulo} ===");
        Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
        Console.WriteLine($"NOPs necessários: {totalNops}");
        if (instrucoesReordenadas.HasValue)
        {
            Console.WriteLine($"Instruções reordenadas: {instrucoesReordenadas.Value}");
        }
        Console.WriteLine($"Sobrecusto: {(double)totalNops / totalInstrucoes * 100:F1}%");
    }

    // Analisa uma instrução hexadecimal e retorna informações relevantes
    public (string hex, string assembly, int rd, int rs1, int rs2) AnalisarInstrucao(string instrucaoHex)
    {
        var instrucaoBinaria = string.Concat(instrucaoHex.Select(c => ConverterHexParaBinario(char.ToUpper(c))));
        var campos = SepararCamposInstrucao(instrucaoBinaria);
        var assembly = IdentificarInstrucaoAssembly(campos.opcode, campos.funct3, campos.funct7);
        var rd = Convert.ToInt32(campos.rd, 2);
        var rs1 = Convert.ToInt32(campos.rs1, 2);
        var rs2 = Convert.ToInt32(campos.rs2, 2);

        return (instrucaoHex, assembly, rd, rs1, rs2);
    }

    // Verifica se uma instrução pode ser executada no slot de atraso de um branch
    public bool PodeExecutarNoSlotDeAtraso((string hex, string assembly, int rd, int rs1, int rs2) branch,
                           (string hex, string assembly, int rd, int rs1, int rs2) proxima)
    {
        // Não pode ser branch/jump e não pode depender do resultado do branch
        return !proxima.assembly.StartsWith("b") && !proxima.assembly.StartsWith("j") && 
               proxima.rs1 != branch.rd && proxima.rs2 != branch.rd;
    }

    // Analisa dependências de registradores de uma instrução
    public (string assembly, int rd, int[] rs) AnalisarDependencias(string instrucaoHex)
    {
        var instrucaoBin = string.Concat(instrucaoHex.Select(c => ConverterHexParaBinario(char.ToUpper(c))));
        var campos = SepararCamposInstrucao(instrucaoBin);
        var assembly = IdentificarInstrucaoAssembly(campos.opcode, campos.funct3, campos.funct7);
        var rd = Convert.ToInt32(campos.rd, 2);
        var rs = new[] {
            Convert.ToInt32(campos.rs1, 2),
            Convert.ToInt32(campos.rs2, 2)
        };
        return (assembly, rd, rs);
    }

    // Inicializa o arquivo de saída e separa os blocos do arquivo de entrada
    public (StringBuilder outputBuilder, List<List<string>> blocos)? InicializarArquivo(string caminhoArquivo, string titulo)
    {
        if (!File.Exists(caminhoArquivo))
        {
            Console.WriteLine("Arquivo não encontrado.");
            return null;
        }

        var outputBuilder = new StringBuilder();
        outputBuilder.AppendLine($"====={titulo}=====");

        var blocos = SepararEmBlocos(caminhoArquivo);
        if (!blocos.Any()) return null;

        return (outputBuilder, blocos);
    }

    // Exibe a sequência original e a nova sequência (modificada) de instruções
    public void ExibirSequencias(StringBuilder outputBuilder, List<string> bloco,
        IEnumerable<(string hex, string assembly, string comentario)> novaSequencia)
    {
        // Exibe sequência original
        ExibirSequenciaOriginal(bloco, outputBuilder);

        // Exibe nova sequência
        outputBuilder.AppendLine("\nSequência modificada:");
        int i = 1;
        foreach (var (hex, assembly, comentario) in novaSequencia)
        {
            var linha = comentario.Length > 0
                ? $"  {i}. {hex} ({assembly}) -> {comentario}"
                : $"  {i}. {hex} ({assembly})";
            outputBuilder.AppendLine(linha);
            i++;
        }
    }
    
    // Processa instruções de um bloco, inserindo NOPs conforme dependências detectadas
    public List<(string hex, string assembly, string comentario)> ProcessarInstrucoes(
        List<string> bloco, Func<(string hex, string assembly, int rd, int rs1, int rs2), int, bool> avaliarDependencia)
    {
        var resultado = new List<(string hex, string assembly, string comentario)>();
        
        for (int j = 0; j < bloco.Count; j++)
        {
            try
            {
                var instrucao = AnalisarInstrucao(bloco[j]);
                resultado.Add((instrucao.hex, instrucao.assembly, ""));

                if (avaliarDependencia(instrucao, j))
                {
                    if (instrucao.assembly.StartsWith("l"))
                    {
                        resultado.Add(("00000000", "nop", "Load-use hazard"));
                    }
                    else
                    {
                        resultado.Add(("00000000", "nop", "Ciclo de espera 1/2"));
                        resultado.Add(("00000000", "nop", "Ciclo de espera 2/2"));
                    }
                }
            }
            catch
            {
                resultado.Add((bloco[j], "erro", "Falha ao analisar instrução"));
            }
        }

        return resultado;
    }

    // Insere NOPs em uma sequência de instruções conforme funções de dependência e quantidade de NOPs
    public List<(string hex, string assembly, bool isNop)> InserirNOPs(
        List<string> bloco, 
        Func<(string hex, string assembly, int rd, int rs1, int rs2), bool> precisaNop,
        Func<(string hex, string assembly, int rd, int rs1, int rs2), int> quantidadeNops,
        Func<(string hex, string assembly, int rd, int rs1, int rs2), string> mensagemNop)
    {
        var resultado = new List<(string hex, string assembly, bool isNop)>();
        
        for (int j = 0; j < bloco.Count; j++)
        {
            var instrucaoAtual = AnalisarInstrucao(bloco[j]);
            resultado.Add((bloco[j], instrucaoAtual.assembly, false));

            if (j < bloco.Count - 1)
            {
                if (precisaNop(instrucaoAtual))
                {
                    int nops = quantidadeNops(instrucaoAtual);
                    for (int n = 0; n < nops; n++)
                    {
                        resultado.Add(("00000000", mensagemNop(instrucaoAtual), true));
                    }
                }
            }
        }
        
        return resultado;
    }
}
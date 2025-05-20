using System.Text;

public class auxFunctions
{
    const string MENSAGEM_ERRO = "Opcode inválido ou não encontrado.";

    public string ConverterHexParaBinario(char caractere)
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

    public string ObterTipoInstrucao(string opcode)
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
            "0110111" => "U-Type",
            "0010111" => "U-Type",
            // Lança uma exceção se o opcode não for reconhecido
            _ => throw new ArgumentException($"Não foi possível identificar a instrução")
        };
    }

    public string IdentificarInstrucaoAssembly(string opcode, string funct3, string funct7)
    {
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

            _ => "Instrução não identificada"
        };
    }

    public (string opcode, string rd, string funct3, string rs1, string rs2, string funct7) SepararCamposInstrucao(string binario)
    {
        if (binario.Length != 32)
        {
            throw new ArgumentException("A instrução deve ter 32 bits");
        }

        // Corrigindo os índices para extrair os campos corretamente
        string opcode = binario.Substring(25, 7);   // bits 6-0
        string rd = binario.Substring(20, 5);       // bits 11-7
        string funct3 = binario.Substring(17, 3);   // bits 14-12
        string rs1 = binario.Substring(12, 5);      // bits 19-15
        string rs2 = binario.Substring(7, 5);       // bits 24-20
        string funct7 = binario.Substring(0, 7);    // bits 31-25

        return (opcode, rd, funct3, rs1, rs2, funct7);
    }

    // Função para exibir o resultado da conversão e identificação
    public void ExibirResultado(string hex, string binario, string tipo, string assembly = "")
    {
        Console.WriteLine($"Hexadecimal: {hex} -> Binário: {binario} -> Tipo: {tipo} -> Assembly: {assembly}");
    }

    // Função para exibir o resumo das instruções identificadas
    public void ExibirResumo(Dictionary<string, int> contador)
    {
        Console.WriteLine("\nResumo das instruções:");
        Console.WriteLine($"R-Type: {contador["R-Type"]}  I-Type: {contador["I-Type"]}  S-Type: {contador["S-Type"]}");
        Console.WriteLine($"B-Type: {contador["B-Type"]}  J-Type: {contador["J-Type"]}  U-Type: {contador["U-Type"]}");
    }

    public void IdentificarInstrucoesBinarias(string caminhoArquivo)
    {
        if (!File.Exists(caminhoArquivo))
        {
            Console.WriteLine("Arquivo não encontrado.");
            return;
        }

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

    public bool PodeExecutarNoSlotDeAtraso((string hex, string assembly, int rd, int rs1, int rs2) branch,
                               (string hex, string assembly, int rd, int rs1, int rs2) proxima)
    {
        // Verifica se a próxima instrução não depende do branch
        return proxima.rs1 != branch.rd && proxima.rs2 != branch.rd;
    }

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

    public void ReportarHazardRAW(StringBuilder sb, int posAtual, string instrAtual, int rd,
        string instrSeguinte, int[] rsSeguinte, bool[] hazards)
    {
        if (hazards.Any(h => h))
        {
            sb.AppendLine($"RAW Hazard detectado:");
            sb.AppendLine($"  Instrução {posAtual} ({instrAtual}) escreve em x{rd}");
            sb.AppendLine($"  Instrução {posAtual + 1} ({instrSeguinte}) lê " +
                (hazards[0] ? $"RS1 (x{rsSeguinte[0]})" : "") +
                (hazards[1] ? $"{(hazards[0] ? " e " : "")}RS2 (x{rsSeguinte[1]})" : ""));
            sb.AppendLine();
        }
    }
    
    public (StringBuilder outputBuilder, List<List<string>> blocos, int totalInstrucoes) InitializeAnalysis(
        string caminhoArquivo, string titulo)
    {
        var outputBuilder = new StringBuilder();
        outputBuilder.AppendLine($"====={titulo}=====");
        var blocos = SepararEmBlocos(caminhoArquivo);
        var totalInstrucoes = blocos.Sum(b => b.Count);
        return (outputBuilder, blocos, totalInstrucoes);
    }
}
using System.Text;

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

string IdentificarInstrucaoAssembly(string opcode, string funct3, string funct7) {
    return (opcode, funct3, funct7) switch {
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

(string opcode, string rd, string funct3, string rs1, string rs2, string funct7) SepararCamposInstrucao(string binario) {
    if (binario.Length != 32) {
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
void ExibirResultado(string hex, string binario, string tipo, string assembly = "") {
    Console.WriteLine($"Hexadecimal: {hex} -> Binário: {binario} -> Tipo: {tipo} -> Assembly: {assembly}");
}

// Função para exibir o resumo das instruções identificadas
void ExibirResumo(Dictionary<string, int> contador) {
    Console.WriteLine("\nResumo das instruções:");
    Console.WriteLine($"R-Type: {contador["R-Type"]}  I-Type: {contador["I-Type"]}  S-Type: {contador["S-Type"]}");
    Console.WriteLine($"B-Type: {contador["B-Type"]}  J-Type: {contador["J-Type"]}  U-Type: {contador["U-Type"]}");
}

(string hex, string assembly, int rd, int rs1, int rs2) AnalisarInstrucao(string instrucaoHex) {
    var instrucaoBinaria = string.Concat(instrucaoHex.Select(c => ConverterHexParaBinario(char.ToUpper(c))));
    var campos = SepararCamposInstrucao(instrucaoBinaria);
    var assembly = IdentificarInstrucaoAssembly(campos.opcode, campos.funct3, campos.funct7);
    var rd = Convert.ToInt32(campos.rd, 2);
    var rs1 = Convert.ToInt32(campos.rs1, 2);
    var rs2 = Convert.ToInt32(campos.rs2, 2);
    
    return (instrucaoHex, assembly, rd, rs1, rs2);
}

void IdentificarInstrucoesBinarias(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)){
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
    
    foreach (var linha in linhas) {
        // Se encontrar uma linha em branco, prepara para o próximo bloco
        if (string.IsNullOrWhiteSpace(linha)) {
            if (!inicioBloco) {
                ExibirResumo(contadorInstrucoes);
                contadorInstrucoes = contadorInstrucoes.ToDictionary(x => x.Key, x => 0);
                blocoAtual++;
                inicioBloco = true;
            }
            continue;
        }

        if (inicioBloco) {
            Console.WriteLine($"\n=== Bloco {blocoAtual} ===");
            inicioBloco = false;
        }

        // Converte cada caractere hexadecimal da linha para binário
        var binarioCompleto = string.Concat(linha.Select(caractere => ConverterHexParaBinario(char.ToUpper(caractere))));

        if (binarioCompleto.Length != 32) {
            ExibirResultado(linha, binarioCompleto, MENSAGEM_ERRO);
            continue;
        }

        try {
            var campos = SepararCamposInstrucao(binarioCompleto);
            var tipoInstrucao = ObterTipoInstrucao(campos.opcode);
            var instrucaoAssembly = IdentificarInstrucaoAssembly(campos.opcode, campos.funct3, campos.funct7);
            
            ExibirResultado(linha, binarioCompleto, tipoInstrucao, instrucaoAssembly);
            
            contadorInstrucoes[tipoInstrucao]++;
        }
        catch (ArgumentException) {
            ExibirResultado(linha, binarioCompleto, MENSAGEM_ERRO);
        }
    }

    // Exibe o resumo do último bloco se houver instruções
    if (!inicioBloco) {
        ExibirResumo(contadorInstrucoes);
    }
}

void EscreverArquivo(string conteudo, string nomeArquivo, string pasta = "outputs") {
    try {
        var diretorio = Path.Combine(".", pasta);
        Directory.CreateDirectory(diretorio);
        var caminhoCompleto = Path.Combine(diretorio, nomeArquivo);
        
        // Limpa o arquivo se ele já existir
        if (File.Exists(caminhoCompleto)) {
            File.WriteAllText(caminhoCompleto, string.Empty);
        }
        
        File.WriteAllText(caminhoCompleto, conteudo);
        Console.WriteLine($"\nArquivo salvo em: {caminhoCompleto}");
    }
    catch (Exception ex) {
        Console.WriteLine($"Erro ao escrever no arquivo: {ex.Message}");
    }
}

void AnalisarRAWHazard(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)) {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var outputBuilder = new StringBuilder();
    var blocos = new List<List<string>>();
    var blocoAtual = new List<string>();

    outputBuilder.AppendLine($"=====RAW Hazard=====");
    
    // Separar o arquivo em blocos
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        if (string.IsNullOrWhiteSpace(linha)) {
            if (blocoAtual.Count > 0) {
                blocos.Add([.. blocoAtual]);
                blocoAtual.Clear();
            }
            continue;
        }
        blocoAtual.Add(linha);
    }
    
    if (blocoAtual.Count > 0) {
        blocos.Add(blocoAtual);
    }

    // Analisar cada bloco
    for (int i = 0; i < blocos.Count; i++) {
        outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
        var bloco = blocos[i];

        for (int j = 0; j < bloco.Count - 2; j++) {
            try {
                var instrucaoAtual = string.Concat(bloco[j].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                var camposAtual = SepararCamposInstrucao(instrucaoAtual);
                var instrucaoAssemblyAtual = IdentificarInstrucaoAssembly(camposAtual.opcode, camposAtual.funct3, camposAtual.funct7);
                int rdAtual = Convert.ToInt32(camposAtual.rd, 2);

                var instrucaoSeguinte1 = string.Concat(bloco[j + 1].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                var camposSeguinte1 = SepararCamposInstrucao(instrucaoSeguinte1);
                var instrucaoAssembly1 = IdentificarInstrucaoAssembly(camposSeguinte1.opcode, camposSeguinte1.funct3, camposSeguinte1.funct7);

                var instrucaoSeguinte2 = string.Concat(bloco[j + 2].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                var camposSeguinte2 = SepararCamposInstrucao(instrucaoSeguinte2);
                var instrucaoAssembly2 = IdentificarInstrucaoAssembly(camposSeguinte2.opcode, camposSeguinte2.funct3, camposSeguinte2.funct7);

                bool hazard1RS1 = Convert.ToInt32(camposSeguinte1.rs1, 2) == rdAtual;
                bool hazard1RS2 = Convert.ToInt32(camposSeguinte1.rs2, 2) == rdAtual;
                bool hazard2RS1 = Convert.ToInt32(camposSeguinte2.rs1, 2) == rdAtual;
                bool hazard2RS2 = Convert.ToInt32(camposSeguinte2.rs2, 2) == rdAtual;

                if (hazard1RS1 || hazard1RS2 || hazard2RS1 || hazard2RS2) {
                    outputBuilder.AppendLine($"RAW Hazard detectado:");
                    outputBuilder.AppendLine($"  Instrução {j + 1} ({instrucaoAssemblyAtual}) escreve em x{rdAtual}");
                    
                    if (hazard1RS1 || hazard1RS2) {
                        outputBuilder.AppendLine($"  Instrução {j + 2} ({instrucaoAssembly1}) lê " + 
                            (hazard1RS1 ? $"RS1 (x{Convert.ToInt32(camposSeguinte1.rs1, 2)})" : "") +
                            (hazard1RS2 ? $"{(hazard1RS1 ? " e " : "")}RS2 (x{Convert.ToInt32(camposSeguinte1.rs2, 2)})" : ""));
                    }
                    if (hazard2RS1 || hazard2RS2) {
                        outputBuilder.AppendLine($"  Instrução {j + 3} ({instrucaoAssembly2}) lê " + 
                            (hazard2RS1 ? $"RS1 (x{Convert.ToInt32(camposSeguinte2.rs1, 2)})" : "") +
                            (hazard2RS2 ? $"{(hazard2RS1 ? " e " : "")}RS2 (x{Convert.ToInt32(camposSeguinte2.rs2, 2)})" : ""));
                    }
                    outputBuilder.AppendLine();
                }
            }
            catch (Exception) {
                outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
            }
        }
    }

    EscreverArquivo(outputBuilder.ToString(), "01-RAW.txt");

    int totalInstrucoes = blocos.Sum(b => b.Count);
    Console.WriteLine($"\n=== Sobrecusto da Análise RAW ===");
    Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
    Console.WriteLine($"NOPs necessários: 0 (Apenas análise)");
    Console.WriteLine($"Sobrecusto: 0%");
}

void AnalisarHazardSemForwarding(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)) {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var outputBuilder = new StringBuilder();
    var blocos = new List<List<string>>();
    var blocoAtual = new List<string>();

    outputBuilder.AppendLine($"=====Sem Forwarding=====");
    
    // Separar o arquivo em blocos
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        if (string.IsNullOrWhiteSpace(linha)) {
            if (blocoAtual.Count > 0) {
                blocos.Add([.. blocoAtual]);
                blocoAtual.Clear();
            }
            continue;
        }
        blocoAtual.Add(linha);
    }
    
    if (blocoAtual.Count > 0) {
        blocos.Add(blocoAtual);
    }

    // Analisar cada bloco
    for (int i = 0; i < blocos.Count; i++) {
        outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
        var bloco = blocos[i];

        for (int j = 0; j < bloco.Count - 1; j++) {
            try {
                var instrucaoAtual = string.Concat(bloco[j].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                var camposAtual = SepararCamposInstrucao(instrucaoAtual);
                var instrucaoAssemblyAtual = IdentificarInstrucaoAssembly(camposAtual.opcode, camposAtual.funct3, camposAtual.funct7);
                int rdAtual = Convert.ToInt32(camposAtual.rd, 2);

                // Verificar as próximas instruções (até 5 instruções à frente)
                int limite = Math.Min(j + 6, bloco.Count);
                for (int k = j + 1; k < limite; k++) {
                    var instrucaoFutura = string.Concat(bloco[k].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                    var camposFutura = SepararCamposInstrucao(instrucaoFutura);
                    var instrucaoAssemblyFutura = IdentificarInstrucaoAssembly(camposFutura.opcode, camposFutura.funct3, camposFutura.funct7);

                    int rs1Futura = Convert.ToInt32(camposFutura.rs1, 2);
                    int rs2Futura = Convert.ToInt32(camposFutura.rs2, 2);

                    if (rs1Futura == rdAtual || rs2Futura == rdAtual) {
                        outputBuilder.AppendLine($"Hazard sem forwarding detectado:");
                        outputBuilder.AppendLine($"  Instrução {j + 1} ({instrucaoAssemblyAtual}) escreve em x{rdAtual}");
                        outputBuilder.AppendLine($"  Instrução {k + 1} ({instrucaoAssemblyFutura}) lê " + 
                            (rs1Futura == rdAtual ? $"RS1 (x{rs1Futura})" : "") +
                            (rs2Futura == rdAtual ? $"{(rs1Futura == rdAtual ? " e " : "")}RS2 (x{rs2Futura})" : ""));
                        outputBuilder.AppendLine($"  Ciclos de espera necessários: {(k - j) * 2}");
                        outputBuilder.AppendLine();
                    }
                }
            }
            catch (Exception) {
                outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
            }
        }
    }

    // Escreve o resultado em um arquivo
    EscreverArquivo(outputBuilder.ToString(), "02-SemForwarding.txt");

    int totalInstrucoes = blocos.Sum(b => b.Count);
    int totalNops = blocos.Sum(bloco => {
        var nops = 0;
        for (int j = 0; j < bloco.Count - 1; j++) {
            var instrucaoAtual = AnalisarInstrucao(bloco[j]);
            if (j + 1 < bloco.Count) {
                var instrucaoSeguinte = AnalisarInstrucao(bloco[j + 1]);
                if (instrucaoSeguinte.rs1 == instrucaoAtual.rd || instrucaoSeguinte.rs2 == instrucaoAtual.rd) {
                    nops += 2; // 2 ciclos de espera necessários
                }
            }
        }
        return nops;
    });

    Console.WriteLine($"\n=== Sobrecusto Sem Forwarding ===");
    Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
    Console.WriteLine($"NOPs necessários: {totalNops}");
    Console.WriteLine($"Sobrecusto: {(double)totalNops / totalInstrucoes * 100:F1}%");
}

void AnalisarHazardComForwarding(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)) {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var outputBuilder = new StringBuilder();
    var blocos = new List<List<string>>();
    var blocoAtual = new List<string>();

    outputBuilder.AppendLine($"=====Com Forwarding=====");
    
    // Separar o arquivo em blocos
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        if (string.IsNullOrWhiteSpace(linha)) {
            if (blocoAtual.Count > 0) {
                blocos.Add([.. blocoAtual]);
                blocoAtual.Clear();
            }
            continue;
        }
        blocoAtual.Add(linha);
    }
    
    if (blocoAtual.Count > 0) {
        blocos.Add(blocoAtual);
    }

    // Analisar cada bloco
    for (int i = 0; i < blocos.Count; i++) {
        outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
        var bloco = blocos[i];

        for (int j = 0; j < bloco.Count - 1; j++) {
            try {
                var instrucaoAtual = string.Concat(bloco[j].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                var camposAtual = SepararCamposInstrucao(instrucaoAtual);
                var instrucaoAssemblyAtual = IdentificarInstrucaoAssembly(camposAtual.opcode, camposAtual.funct3, camposAtual.funct7);
                int rdAtual = Convert.ToInt32(camposAtual.rd, 2);

                // Verificar até 5 instruções à frente
                int limite = Math.Min(j + 6, bloco.Count);
                for (int k = j + 1; k < limite; k++) {
                    var instrucaoFutura = string.Concat(bloco[k].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                    var camposFutura = SepararCamposInstrucao(instrucaoFutura);
                    var instrucaoAssemblyFutura = IdentificarInstrucaoAssembly(camposFutura.opcode, camposFutura.funct3, camposFutura.funct7);

                    int rs1Futura = Convert.ToInt32(camposFutura.rs1, 2);
                    int rs2Futura = Convert.ToInt32(camposFutura.rs2, 2);

                    bool isLoad = instrucaoAssemblyAtual.StartsWith("l");
                    bool isImmediate = k == j + 1 && (rs1Futura == rdAtual || rs2Futura == rdAtual);

                    if (rs1Futura == rdAtual || rs2Futura == rdAtual) {
                        outputBuilder.AppendLine($"Dependência detectada:");
                        outputBuilder.AppendLine($"  Instrução {j + 1} ({instrucaoAssemblyAtual}) escreve em x{rdAtual}");
                        outputBuilder.AppendLine($"  Instrução {k + 1} ({instrucaoAssemblyFutura}) lê " + 
                            (rs1Futura == rdAtual ? $"RS1 (x{rs1Futura})" : "") +
                            (rs2Futura == rdAtual ? $"{(rs1Futura == rdAtual ? " e " : "")}RS2 (x{rs2Futura})" : ""));

                        if (isLoad && isImmediate) {
                            outputBuilder.AppendLine("  Tipo: Load-use hazard (não resolvido por forwarding)");
                            outputBuilder.AppendLine("  Ciclos de espera necessários: 1");
                        } else {
                            outputBuilder.AppendLine("  Tipo: ALU-use hazard (resolvido por forwarding)");
                            outputBuilder.AppendLine("  Ciclos de espera necessários: 0");
                        }
                        outputBuilder.AppendLine();
                    }
                }
            }
            catch (Exception) {
                outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
            }
        }
    }

    EscreverArquivo(outputBuilder.ToString(), "03-ComForwarding.txt");

    int totalInstrucoes = blocos.Sum(b => b.Count);
    int totalNops = blocos.Sum(bloco => {
        var nops = 0;
        for (int j = 0; j < bloco.Count - 1; j++) {
            var instrucaoAtual = AnalisarInstrucao(bloco[j]);
            if (instrucaoAtual.assembly.StartsWith("l") && j + 1 < bloco.Count) {
                var instrucaoSeguinte = AnalisarInstrucao(bloco[j + 1]);
                if (instrucaoSeguinte.rs1 == instrucaoAtual.rd || instrucaoSeguinte.rs2 == instrucaoAtual.rd) {
                    nops++; // 1 ciclo apenas para load-use hazards
                }
            }
        }
        return nops;
    });

    Console.WriteLine($"\n=== Sobrecusto Com Forwarding ===");
    Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
    Console.WriteLine($"NOPs necessários: {totalNops}");
    Console.WriteLine($"Sobrecusto: {(double)totalNops / totalInstrucoes * 100:F1}%");
}

void AnalisarHazardComNOP(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)) {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var outputBuilder = new StringBuilder();
    var blocos = new List<List<string>>();
    var blocoAtual = new List<string>();

    outputBuilder.AppendLine($"=====Com NOPs=====");
    
    // Separar o arquivo em blocos
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        if (string.IsNullOrWhiteSpace(linha)) {
            if (blocoAtual.Count > 0) {
                blocos.Add([.. blocoAtual]);
                blocoAtual.Clear();
            }
            continue;
        }
        blocoAtual.Add(linha);
    }
    
    if (blocoAtual.Count > 0) {
        blocos.Add(blocoAtual);
    }

    // Analisar cada bloco
    for (int i = 0; i < blocos.Count; i++) {
        outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
        var bloco = blocos[i];
        var novaSequencia = new List<(string hex, string assembly, bool isNop)>();

        for (int j = 0; j < bloco.Count; j++) {
            try {
                var instrucaoAtual = string.Concat(bloco[j].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                var camposAtual = SepararCamposInstrucao(instrucaoAtual);
                var instrucaoAssemblyAtual = IdentificarInstrucaoAssembly(camposAtual.opcode, camposAtual.funct3, camposAtual.funct7);
                int rdAtual = Convert.ToInt32(camposAtual.rd, 2);

                novaSequencia.Add((bloco[j], instrucaoAssemblyAtual, false));

                if (j + 1 < bloco.Count) {
                    var instrucaoSeguinte = string.Concat(bloco[j + 1].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                    var camposSeguinte = SepararCamposInstrucao(instrucaoSeguinte);
                    int rs1Seguinte = Convert.ToInt32(camposSeguinte.rs1, 2);
                    int rs2Seguinte = Convert.ToInt32(camposSeguinte.rs2, 2);

                    if (rs1Seguinte == rdAtual || rs2Seguinte == rdAtual) {
                        bool isLoad = instrucaoAssemblyAtual.StartsWith("l");

                        if (isLoad) {
                            novaSequencia.Add(("00000000", $"(NOP) -> Load {instrucaoAssemblyAtual} precisa de 1 ciclo para buscar x{rdAtual} da memória", true));
                        } else {
                            novaSequencia.Add(("00000000", $"(NOP) -> {instrucaoAssemblyAtual} ainda não escreveu x{rdAtual} na ALU (ciclo 1/2)", true));
                            novaSequencia.Add(("00000000", $"(NOP) -> {instrucaoAssemblyAtual} ainda não escreveu x{rdAtual} na ALU (ciclo 2/2)", true));
                        }
                    }
                }
            }
            catch (Exception) {
                outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
            }
        }

        // Exibe a sequência original
        outputBuilder.AppendLine("Sequência original:");
        for (int j = 0; j < bloco.Count; j++) {
            var (_, assembly, _, _, _) = AnalisarInstrucao(bloco[j]);
            outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
        }

        // Exibe a sequência com NOPs
        outputBuilder.AppendLine("\nSequência com NOPs:");
        for (int j = 0; j < novaSequencia.Count; j++) {
            var (hex, assembly, isNop) = novaSequencia[j];
            if (isNop) {
                outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
            } else {
                outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly})");
            }
        }
        outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {novaSequencia.Count(x => x.isNop)}");
        outputBuilder.AppendLine();
    }

    // Calcular o sobrecusto total
    int totalInstrucoes = blocos.Sum(b => b.Count);
    int totalNops = blocos.Sum(bloco => {
        // Conta NOPs necessários para cada instrução no bloco
        var nops = 0;
        for (int j = 0; j < bloco.Count - 1; j++) {
            var instrucaoAtual = AnalisarInstrucao(bloco[j]);
            if (j + 1 < bloco.Count) {
                var instrucaoSeguinte = AnalisarInstrucao(bloco[j + 1]);
                if (instrucaoSeguinte.rs1 == instrucaoAtual.rd || instrucaoSeguinte.rs2 == instrucaoAtual.rd) {
                    nops += instrucaoAtual.assembly.StartsWith("l") ? 1 : 2;
                }
            }
        }
        return nops;
    });
    
    EscreverArquivo(outputBuilder.ToString(), "04-ComNOPs.txt");

    Console.WriteLine($"\n=== Sobrecusto Com NOPs ===");
    Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
    Console.WriteLine($"NOPs necessários: {totalNops}");
    Console.WriteLine($"Sobrecusto: {(double)totalNops / totalInstrucoes * 100:F1}%");

}

void AnalisarHazardComForwardingENOP(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)) {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var outputBuilder = new StringBuilder();
    var blocos = new List<List<string>>();
    var blocoAtual = new List<string>();

    outputBuilder.AppendLine($"=====Com Forwarding + NOPs=====");
    
    // Separar o arquivo em blocos
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        if (string.IsNullOrWhiteSpace(linha)) {
            if (blocoAtual.Count > 0) {
                blocos.Add([.. blocoAtual]);
                blocoAtual.Clear();
            }
            continue;
        }
        blocoAtual.Add(linha);
    }
    
    if (blocoAtual.Count > 0) {
        blocos.Add(blocoAtual);
    }

    // Analisar cada bloco
    for (int i = 0; i < blocos.Count; i++) {
        outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
        var bloco = blocos[i];
        var novaSequencia = new List<(string hex, string assembly, bool isNop)>();

        for (int j = 0; j < bloco.Count; j++) {
            try {
                var instrucaoAtual = string.Concat(bloco[j].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                var camposAtual = SepararCamposInstrucao(instrucaoAtual);
                var instrucaoAssemblyAtual = IdentificarInstrucaoAssembly(camposAtual.opcode, camposAtual.funct3, camposAtual.funct7);
                int rdAtual = Convert.ToInt32(camposAtual.rd, 2);

                novaSequencia.Add((bloco[j], instrucaoAssemblyAtual, false));

                if (j + 1 < bloco.Count) {
                    var instrucaoSeguinte = string.Concat(bloco[j + 1].Select(c => ConverterHexParaBinario(char.ToUpper(c))));
                    var camposSeguinte = SepararCamposInstrucao(instrucaoSeguinte);
                    int rs1Seguinte = Convert.ToInt32(camposSeguinte.rs1, 2);
                    int rs2Seguinte = Convert.ToInt32(camposSeguinte.rs2, 2);

                    bool hazardImediato = rs1Seguinte == rdAtual || rs2Seguinte == rdAtual;

                    if (hazardImediato) {
                        bool isLoad = instrucaoAssemblyAtual.StartsWith("l");

                        if (isLoad) {
                            novaSequencia.Add(("00000000", $"(NOP) -> Load {instrucaoAssemblyAtual} precisa de 1 ciclo para buscar x{rdAtual} da memória", true));
                        } else {
                            novaSequencia.Add(("00000000", $"(NOP) -> {instrucaoAssemblyAtual} ainda não escreveu x{rdAtual} na ALU (ciclo 1/2)", true));
                            novaSequencia.Add(("00000000", $"(NOP) -> {instrucaoAssemblyAtual} ainda não escreveu x{rdAtual} na ALU (ciclo 2/2)", true));
                        }
                    }
                }
            }
            catch (Exception) {
                outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
            }
        }

        // Exibe a sequência original
        outputBuilder.AppendLine("Sequência original:");
        for (int j = 0; j < bloco.Count; j++) {
            var (_, assembly, _, _, _) = AnalisarInstrucao(bloco[j]);
            outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
        }

        // Exibe a sequência com Forwarding + NOPs
        outputBuilder.AppendLine("\nSequência com Forwarding + NOPs:");
        for (int j = 0; j < novaSequencia.Count; j++) {
            var (hex, assembly, isNop) = novaSequencia[j];
            if (isNop) {
                outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
            } else {
                outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly})");
            }
        }
        outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {novaSequencia.Count(x => x.isNop)}");
        outputBuilder.AppendLine();
    }

    // Calcular e exibir sobrecusto
    int totalInstrucoes = blocos.Sum(b => b.Count);
    int totalNops = blocos.Sum(bloco => {
        var nops = 0;
        for (int j = 0; j < bloco.Count - 1; j++) {
            var instrucaoAtual = AnalisarInstrucao(bloco[j]);
            if (instrucaoAtual.assembly.StartsWith("l") && j + 1 < bloco.Count) {
                var instrucaoSeguinte = AnalisarInstrucao(bloco[j + 1]);
                if (instrucaoSeguinte.rs1 == instrucaoAtual.rd || instrucaoSeguinte.rs2 == instrucaoAtual.rd) {
                    nops++; // 1 ciclo apenas para load-use hazards
                }
            }
        }
        return nops;
    });

    Console.WriteLine($"\n=== Sobrecusto Com Forwarding e NOPs ===");
    Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
    Console.WriteLine($"NOPs necessários: {totalNops}");
    Console.WriteLine($"Sobrecusto: {(double)totalNops / totalInstrucoes * 100:F1}%");
}

void AnalisarHazardComReordenacao(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)) {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var outputBuilder = new StringBuilder();
    var blocos = new List<List<string>>();
    var blocoAtual = new List<string>();

    outputBuilder.AppendLine($"=====Com Reordenação de Instruções=====");
    
    // Separar o arquivo em blocos
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        if (string.IsNullOrWhiteSpace(linha)) {
            if (blocoAtual.Count > 0) {
                blocos.Add([.. blocoAtual]);
                blocoAtual.Clear();
            }
            continue;
        }
        blocoAtual.Add(linha);
    }
    
    if (blocoAtual.Count > 0) {
        blocos.Add(blocoAtual);
    }

    // Analisar cada bloco
    int totalInstrucoes = 0;
    int totalNops = 0;
    int totalReordenadas = 0;

    for (int i = 0; i < blocos.Count; i++) {
        outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
        var bloco = blocos[i];
        totalInstrucoes += bloco.Count;
        
        // Lista de instruções com suas dependências
        var instrucoes = new List<(string hex, string assembly, int rd, HashSet<int> dependencias, int posOriginal)>();
        
        // Primeira passagem: identificar todas as instruções e suas dependências
        for (int j = 0; j < bloco.Count; j++) {
            try {
                var (hex, assembly, rd, rs1, rs2) = AnalisarInstrucao(bloco[j]);
                instrucoes.Add((hex, assembly, rd, new HashSet<int> { rs1, rs2 }, j));
            }
            catch (Exception) {
                outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
            }
        }

        // Reordenar instruções para minimizar dependências
        var reordenadas = new List<(string hex, string assembly, bool isNop, int posOriginal)>();
        var registradoresEmUso = new HashSet<int>();
        var instrucoesRestantes = new List<(string hex, string assembly, int rd, HashSet<int> dependencias, int posOriginal)>(instrucoes);

        while (instrucoesRestantes.Any()) {
            bool encontrouInstrucaoIndependente = false;
            
            // Procura por uma instrução que não dependa dos registradores em uso
            for (int j = 0; j < instrucoesRestantes.Count; j++) {
                var instrucao = instrucoesRestantes[j];
                if (!instrucao.dependencias.Overlaps(registradoresEmUso)) {
                    if (reordenadas.Count != instrucao.posOriginal) totalReordenadas++;
                    reordenadas.Add((instrucao.hex, instrucao.assembly, false, instrucao.posOriginal));
                    registradoresEmUso.Add(instrucao.rd);
                    instrucoesRestantes.RemoveAt(j);
                    encontrouInstrucaoIndependente = true;
                    break;
                }
            }

            // Se não encontrou instrução independente, adiciona NOP e pega a próxima da fila
            if (!encontrouInstrucaoIndependente && instrucoesRestantes.Any()) {
                totalNops++;
                reordenadas.Add(("00000000", $"(NOP) -> Aguardando x{string.Join(",x", instrucoesRestantes[0].dependencias)} ficar disponível", true, -1));
                var proxima = instrucoesRestantes[0];
                instrucoesRestantes.RemoveAt(0);
                reordenadas.Add((proxima.hex, proxima.assembly, false, proxima.posOriginal));
                registradoresEmUso.Add(proxima.rd);
            }
        }

        // Exibe a sequência original
        outputBuilder.AppendLine("Sequência original:");
        for (int j = 0; j < bloco.Count; j++) {
            var (_, assembly, _, _, _) = AnalisarInstrucao(bloco[j]);
            outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
        }

        // Exibe a sequência reordenada
        outputBuilder.AppendLine("\nSequência reordenada (com NOPs quando necessário):");
        for (int j = 0; j < reordenadas.Count; j++) {
            var (hex, assembly, isNop, posOriginal) = reordenadas[j];
            if (isNop) {
                outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
            } else {
                var indicadorReordenacao = posOriginal != j ? $" -> [Movida da posição {posOriginal + 1}]" : "";
                outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly}){indicadorReordenacao}");
            }
        }
        
        outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {reordenadas.Count(x => x.isNop)}");
        outputBuilder.AppendLine();
    }

    EscreverArquivo(outputBuilder.ToString(), "06-ComReordenacao.txt");

    Console.WriteLine($"\n=== Sobrecusto Com Reordenação ===");
    Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
    Console.WriteLine($"NOPs necessários: {totalNops}");
    Console.WriteLine($"Instruções reordenadas: {totalReordenadas}");
    Console.WriteLine($"Sobrecusto: {(double)totalNops / totalInstrucoes * 100:F1}%");
}

void AnalisarHazardComForwardingEReordenacao(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)) {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var outputBuilder = new StringBuilder();
    var blocos = new List<List<string>>();
    var blocoAtual = new List<string>();

    outputBuilder.AppendLine($"=====Com Forwarding + Reordenação=====");
    
    // Separar o arquivo em blocos
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        if (string.IsNullOrWhiteSpace(linha)) {
            if (blocoAtual.Count > 0) {
                blocos.Add([.. blocoAtual]);
                blocoAtual.Clear();
            }
            continue;
        }
        blocoAtual.Add(linha);
    }
    
    if (blocoAtual.Count > 0) {
        blocos.Add(blocoAtual);
    }

    // Analisar cada bloco
    int totalInstrucoes = 0;
    int totalNops = 0;
    int totalReordenadas = 0;

    // Analisar cada bloco
    for (int i = 0; i < blocos.Count; i++) {
        outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
        var bloco = blocos[i];
        totalInstrucoes += bloco.Count;
        
        // Lista de instruções com suas dependências
        var instrucoes = new List<(string hex, string assembly, bool isLoad, int rd, HashSet<int> dependencias, int posOriginal)>();
        
        // Primeira passagem: identificar todas as instruções e suas dependências
        for (int j = 0; j < bloco.Count; j++) {
            try {
                var (hex, assembly, rd, rs1, rs2) = AnalisarInstrucao(bloco[j]);
                var isLoad = assembly.StartsWith("l");
                instrucoes.Add((hex, assembly, isLoad, rd, new HashSet<int> { rs1, rs2 }, j));
            }
            catch (Exception) {
                outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
            }
        }

        // Reordenar instruções considerando forwarding
        var reordenadas = new List<(string hex, string assembly, bool isNop, int posOriginal)>();
        var registradoresEmUso = new Dictionary<int, bool>(); // rd -> isLoad
        var instrucoesRestantes = new List<(string hex, string assembly, bool isLoad, int rd, HashSet<int> dependencias, int posOriginal)>(instrucoes);

        while (instrucoesRestantes.Any()) {
            bool encontrouInstrucaoValida = false;
            
            // Procura por uma instrução que possa ser executada com forwarding
            for (int j = 0; j < instrucoesRestantes.Count; j++) {
                var instrucao = instrucoesRestantes[j];
                bool temDependenciaLoad = instrucao.dependencias.Any(dep => 
                    registradoresEmUso.ContainsKey(dep) && registradoresEmUso[dep]);
                
                if (!temDependenciaLoad) {
                    if (reordenadas.Count != instrucao.posOriginal) totalReordenadas++;
                    reordenadas.Add((instrucao.hex, instrucao.assembly, false, instrucao.posOriginal));
                    if (registradoresEmUso.ContainsKey(instrucao.rd)) {
                        registradoresEmUso[instrucao.rd] = instrucao.isLoad;
                    } else {
                        registradoresEmUso.Add(instrucao.rd, instrucao.isLoad);
                    }
                    instrucoesRestantes.RemoveAt(j);
                    encontrouInstrucaoValida = true;
                    break;
                }
            }

            // Se não encontrou instrução válida, adiciona NOP e a próxima instrução
            if (!encontrouInstrucaoValida && instrucoesRestantes.Any()) {
                var proxima = instrucoesRestantes[0];
                if (proxima.dependencias.Any(dep => registradoresEmUso.ContainsKey(dep) && registradoresEmUso[dep])) {
                    totalNops++;
                    reordenadas.Add(("00000000", $"(NOP) -> Aguardando load para x{string.Join(",x", proxima.dependencias.Where(d => registradoresEmUso.ContainsKey(d) && registradoresEmUso[d]))}", true, -1));
                }
                instrucoesRestantes.RemoveAt(0);
                reordenadas.Add((proxima.hex, proxima.assembly, false, proxima.posOriginal));
                registradoresEmUso[proxima.rd] = proxima.isLoad;
            }

            // Limpa registradores que não são mais necessários
            var regsParaRemover = registradoresEmUso.Keys
                .Where(reg => !instrucoesRestantes.Any(i => i.dependencias.Contains(reg)))
                .ToList();
            foreach (var reg in regsParaRemover) {
                registradoresEmUso.Remove(reg);
            }
        }

        // Exibe a sequência original
        outputBuilder.AppendLine("Sequência original:");
        for (int j = 0; j < bloco.Count; j++) {
            var (_, assembly, _, _, _) = AnalisarInstrucao(bloco[j]);
            outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
        }

        // Exibe a sequência com Forwarding + Reordenação:
        outputBuilder.AppendLine("\nSequência com Forwarding + Reordenação:");
        for (int j = 0; j < reordenadas.Count; j++) {
            var (hex, assembly, isNop, posOriginal) = reordenadas[j];
            if (isNop) {
                outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
            } else {
                var indicadorReordenacao = posOriginal != j ? $" -> [Movida da posição {posOriginal + 1}]" : "";
                outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly}){indicadorReordenacao}");
            }
        }
        
        outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {reordenadas.Count(x => x.isNop)}");
        outputBuilder.AppendLine();
    }

    EscreverArquivo(outputBuilder.ToString(), "07-ComForwardingEReordenacao.txt");

    Console.WriteLine($"\n=== Sobrecusto Com Forwarding e Reordenação ===");
    Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
    Console.WriteLine($"NOPs necessários: {totalNops}");
    Console.WriteLine($"Instruções reordenadas: {totalReordenadas}");
    Console.WriteLine($"Sobrecusto: {(double)totalNops / totalInstrucoes * 100:F1}%");
}

void AnalisarHazardDeControle(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)) {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var outputBuilder = new StringBuilder();
    var blocos = new List<List<string>>();
    var blocoAtual = new List<string>();

    outputBuilder.AppendLine($"=====Hazards de Controle=====");
    
    // Separar o arquivo em blocos
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        if (string.IsNullOrWhiteSpace(linha)) {
            if (blocoAtual.Count > 0) {
                blocos.Add([.. blocoAtual]);
                blocoAtual.Clear();
            }
            continue;
        }
        blocoAtual.Add(linha);
    }
    
    if (blocoAtual.Count > 0) {
        blocos.Add(blocoAtual);
    }

    // Analisar cada bloco
    for (int i = 0; i < blocos.Count; i++) {
        outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
        var bloco = blocos[i];
        var novaSequencia = new List<(string hex, string assembly, bool isNop)>();
        
        for (int j = 0; j < bloco.Count; j++) {
            try {
                var (hex, assembly, _, _, _) = AnalisarInstrucao(bloco[j]);
                bool isJump = assembly.StartsWith("j");
                bool isBranch = assembly.StartsWith("b");
                
                novaSequencia.Add((hex, assembly, false));

                if (isJump) {
                    outputBuilder.AppendLine($"\nHazard de controle detectado após instrução {j + 1} ({assembly}):");
                    outputBuilder.AppendLine("  Inserindo 1 NOP para cálculo do endereço alvo\n");
                    novaSequencia.Add(("00000000", "(NOP) -> Aguardando cálculo do endereço alvo do jump", true));
                }
                else if (isBranch) {
                    outputBuilder.AppendLine($"\nHazard de controle detectado após instrução {j + 1} ({assembly}):");
                    outputBuilder.AppendLine("  Inserindo 2 NOPs para:");
                    outputBuilder.AppendLine("  1. Aguardar avaliação da condição");
                    outputBuilder.AppendLine("  2. Calcular endereço alvo\n");
                    
                    novaSequencia.Add(("00000000", "(NOP) -> Aguardando avaliação da condição do branch", true));
                    novaSequencia.Add(("00000000", "(NOP) -> Aguardando cálculo do endereço alvo", true));
                }
            }
            catch (Exception) {
                outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
            }
        }

        // Exibe a sequência original
        outputBuilder.AppendLine("Sequência original:");
        for (int j = 0; j < bloco.Count; j++) {
            var (_, assembly, _, _, _) = AnalisarInstrucao(bloco[j]);
            outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
        }

        // Exibe a sequência com NOPs para controle
        outputBuilder.AppendLine("\nSequência com NOPs para hazards de controle:");
        for (int j = 0; j < novaSequencia.Count; j++) {
            var (hex, assembly, isNop) = novaSequencia[j];
            if (isNop) {
                outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
            } else {
                outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly})");
            }
        }
        
        outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {novaSequencia.Count(x => x.isNop)}");
        outputBuilder.AppendLine();
    }

    EscreverArquivo(outputBuilder.ToString(), "08-HazardsDeControle.txt");

    int totalInstrucoes = blocos.Sum(b => b.Count);
    int totalNops = blocos.Sum(bloco => {
        var nops = 0;
        foreach (var linha in bloco) {
            var instrucao = AnalisarInstrucao(linha);
            if (instrucao.assembly.StartsWith("j")) nops += 1;
            else if (instrucao.assembly.StartsWith("b")) nops += 2;
        }
        return nops;
    });

    Console.WriteLine($"\n=== Sobrecusto Hazards de Controle ===");
    Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
    Console.WriteLine($"NOPs necessários: {totalNops}");
    Console.WriteLine($"Sobrecusto: {(double)totalNops / totalInstrucoes * 100:F1}%");
}

void AnalisarHazardComDelayedBranch(string caminhoArquivo) {
    if (!File.Exists(caminhoArquivo)) {
        Console.WriteLine("Arquivo não encontrado.");
        return;
    }

    var outputBuilder = new StringBuilder();
    var blocos = new List<List<string>>();
    var blocoAtual = new List<string>();

    outputBuilder.AppendLine($"=====Com Delayed Branch=====");
    
    // Separar o arquivo em blocos
    foreach (var linha in File.ReadLines(caminhoArquivo)) {
        if (string.IsNullOrWhiteSpace(linha)) {
            if (blocoAtual.Count > 0) {
                blocos.Add([.. blocoAtual]);
                blocoAtual.Clear();
            }
            continue;
        }
        blocoAtual.Add(linha);
    }
    
    if (blocoAtual.Count > 0) {
        blocos.Add(blocoAtual);
    }

    // Analisar cada bloco
    for (int i = 0; i < blocos.Count; i++) {
        outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
        var bloco = blocos[i];
        var novaSequencia = new List<(string hex, string assembly, string comentario)>();
        
        for (int j = 0; j < bloco.Count; j++) {
            try {
                var (hex, assembly, rd, rs1, rs2) = AnalisarInstrucao(bloco[j]);
                bool isBranch = assembly.StartsWith("b");
                bool isJump = assembly.StartsWith("j");

                if (isBranch || isJump) {
                    novaSequencia.Add((hex, assembly, "Instrução de desvio"));
                    
                    // Tenta encontrar uma instrução independente nas próximas instruções
                    bool encontrouIndependente = false;
                    if (j + 1 < bloco.Count) {
                        var (nextHex, nextAssembly, nextRd, nextRs1, nextRs2) = AnalisarInstrucao(bloco[j + 1]);
                        
                        // Verifica se a próxima instrução é independente do branch/jump
                        bool isDependente = nextRs1 == rd || nextRs2 == rd;
                        
                        if (!isDependente) {
                            novaSequencia.Add((nextHex, nextAssembly, "Instrução independente movida para slot de atraso"));
                            j++; // Pula a próxima instrução já que foi movida
                            encontrouIndependente = true;
                        }
                    }
                    
                    if (!encontrouIndependente) {
                        novaSequencia.Add(("00000000", "nop", "NOP inserido no slot de atraso do branch/jump"));
                    }
                } else {
                    novaSequencia.Add((hex, assembly, ""));
                }
            }
            catch (Exception) {
                outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
            }
        }

        // Exibe a sequência original
        outputBuilder.AppendLine("Sequência original:");
        for (int j = 0; j < bloco.Count; j++) {
            var (_, assembly, _, _, _) = AnalisarInstrucao(bloco[j]);
            outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
        }

        // Exibe a sequência com delayed branch
        outputBuilder.AppendLine("\nSequência com Delayed Branch:");
        for (int j = 0; j < novaSequencia.Count; j++) {
            var (hex, assembly, comentario) = novaSequencia[j];
            if (comentario != "") {
                outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly}) -> {comentario}");
            } else {
                outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly})");
            }
        }
        
        var nopsInseridos = novaSequencia.Count(x => x.assembly == "nop");
        var instrucoesMovidas = novaSequencia.Count(x => x.comentario.Contains("movida"));
        
        outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {nopsInseridos}");
        outputBuilder.AppendLine($"Total de instruções reordenadas: {instrucoesMovidas}");
        outputBuilder.AppendLine();
    }

    EscreverArquivo(outputBuilder.ToString(), "09-DelayedBranch.txt");

    int totalInstrucoes = blocos.Sum(b => b.Count);
    int totalNops = blocos.Sum(bloco => {
        var nops = 0;
        for (int j = 0; j < bloco.Count; j++) {
            var instrucao = AnalisarInstrucao(bloco[j]);
            if ((instrucao.assembly.StartsWith("b") || instrucao.assembly.StartsWith("j")) &&
                (j + 1 >= bloco.Count || !PodeExecutarNoSlotDeAtraso(instrucao, AnalisarInstrucao(bloco[j + 1])))) {
                nops++;
            }
        }
        return nops;
    });

    Console.WriteLine($"\n=== Sobrecusto Com Delayed Branch ===");
    Console.WriteLine($"Total de instruções originais: {totalInstrucoes}");
    Console.WriteLine($"NOPs necessários: {totalNops}");
    Console.WriteLine($"Sobrecusto: {(double)totalNops / totalInstrucoes * 100:F1}%");
    Console.WriteLine($"Instruções reordenadas: {totalInstrucoes - totalNops}");
}

bool PodeExecutarNoSlotDeAtraso((string hex, string assembly, int rd, int rs1, int rs2) branch, 
                               (string hex, string assembly, int rd, int rs1, int rs2) proxima) {
    // Verifica se a próxima instrução não depende do branch
    return proxima.rs1 != branch.rd && proxima.rs2 != branch.rd;
}

// Chama as funções principais para processar o arquivo
IdentificarInstrucoesBinarias(caminhoArquivo);
AnalisarRAWHazard(caminhoArquivo);
AnalisarHazardSemForwarding(caminhoArquivo);
AnalisarHazardComForwarding(caminhoArquivo);
AnalisarHazardComNOP(caminhoArquivo);
AnalisarHazardComForwardingENOP(caminhoArquivo);
AnalisarHazardComReordenacao(caminhoArquivo);
AnalisarHazardComForwardingEReordenacao(caminhoArquivo);
AnalisarHazardDeControle(caminhoArquivo);
AnalisarHazardComDelayedBranch(caminhoArquivo);
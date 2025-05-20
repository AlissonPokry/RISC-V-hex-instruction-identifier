using System.Text;

// Classe responsável pela análise de diferentes tipos de hazards em instruções RISC-V
public class HazardAnalysis
{
    private readonly auxFunctions aux = new auxFunctions();

    // Exibe as sequências de instruções antes e depois da análise de hazards
    private void ExibirSequencias(StringBuilder outputBuilder, List<string> bloco, 
        IEnumerable<(string hex, string assembly, string comentario)> novaSequencia)
    {
        // Exibe sequência original
        outputBuilder.AppendLine("Sequência original:");
        for (int j = 0; j < bloco.Count; j++)
        {
            var (_, assembly, _, _, _) = aux.AnalisarInstrucao(bloco[j]);
            outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
        }

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

    // Processa as instruções do bloco, inserindo NOPs quando necessário
    // avaliarDependencia: função que determina se existe dependência entre instruções
    private List<(string hex, string assembly, string comentario)> ProcessarInstrucoes(
        List<string> bloco, Func<(string hex, string assembly, int rd, int rs1, int rs2), int, bool> avaliarDependencia)
    {
        var resultado = new List<(string hex, string assembly, string comentario)>();
        
        for (int j = 0; j < bloco.Count; j++)
        {
            try
            {
                var instrucao = aux.AnalisarInstrucao(bloco[j]);
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

    // Detecta hazards do tipo Read After Write (RAW)
    // Identifica quando uma instrução tenta ler um registrador antes que seu valor seja escrito
    public void AnalisarRAWHazard(string caminhoArquivo)
    {
        var (outputBuilder, blocos, totalInstrucoes) = aux.InitializeAnalysis(caminhoArquivo, "RAW Hazard");
        if (!blocos.Any()) return;

        foreach (var (bloco, i) in blocos.Select((b, i) => (b, i)))
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            
            var sequencia = ProcessarInstrucoes(bloco, (instrucao, j) => 
                j + 1 < bloco.Count && (aux.AnalisarInstrucao(bloco[j + 1]).rs1 == instrucao.rd || aux.AnalisarInstrucao(bloco[j + 1]).rs2 == instrucao.rd));
                
            ExibirSequencias(outputBuilder, bloco, sequencia);
        }

        aux.EscreverArquivo(outputBuilder.ToString(), "01-RAW.txt");
        aux.ExibirSobrecusto("Análise RAW", totalInstrucoes, 0);
    }

    // Analisa hazards sem utilizar forwarding
    // Requer ciclos completos de espera entre instruções dependentes
    public void AnalisarHazardSemForwarding(string caminhoArquivo)
    {
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Sem Forwarding");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];

            // Exibe a sequência original
            aux.ExibirSequenciaOriginal(bloco, outputBuilder);
            outputBuilder.AppendLine("\nDependências detectadas:");

            for (int j = 0; j < bloco.Count - 1; j++)
            {
                try
                {
                    var instrucaoAtual = aux.AnalisarInstrucao(bloco[j]);
                    int rdAtual = instrucaoAtual.rd;

                    // Verificar até 5 instruções à frente
                    int limite = Math.Min(j + 6, bloco.Count);
                    for (int k = j + 1; k < limite; k++)
                    {
                        var instrucaoFutura = aux.AnalisarInstrucao(bloco[k]);
                        if (instrucaoFutura.rs1 == rdAtual || instrucaoFutura.rs2 == rdAtual)
                        {
                            outputBuilder.AppendLine($"  Instrução {j + 1} ({instrucaoAtual.assembly}) escreve em x{rdAtual}");
                            outputBuilder.AppendLine($"  Instrução {k + 1} ({instrucaoFutura.assembly}) lê " +
                                (instrucaoFutura.rs1 == rdAtual ? $"RS1 (x{instrucaoFutura.rs1})" : "") +
                                (instrucaoFutura.rs2 == rdAtual ? $"{(instrucaoFutura.rs1 == rdAtual ? " e " : "")}RS2 (x{instrucaoFutura.rs2})" : ""));
                            outputBuilder.AppendLine($"  Ciclos de espera necessários: {(k - j) * 2}");
                            outputBuilder.AppendLine();
                        }
                    }
                }
                catch (Exception)
                {
                    outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
                }
            }
        }

        // Escreve o resultado em um arquivo
        aux.EscreverArquivo(outputBuilder.ToString(), "02-SemForwarding.txt");

        int totalInstrucoes = blocos.Sum(b => b.Count);
        int totalNops = blocos.Sum(bloco =>
        {
            var nops = 0;
            for (int j = 0; j < bloco.Count - 1; j++)
            {
                var instrucaoAtual = aux.AnalisarInstrucao(bloco[j]);
                if (j + 1 < bloco.Count)
                {
                    var instrucaoSeguinte = aux.AnalisarInstrucao(bloco[j + 1]);
                    if (instrucaoSeguinte.rs1 == instrucaoAtual.rd || instrucaoSeguinte.rs2 == instrucaoAtual.rd)
                    {
                        nops += 2; // 2 ciclos de espera necessários
                    }
                }
            }
            return nops;
        });

        aux.ExibirSobrecusto("Sem Forwarding", totalInstrucoes, totalNops);
    }

    // Analisa hazards utilizando técnica de forwarding
    // Permite reduzir ciclos de espera encaminhando resultados entre estágios do pipeline
    public void AnalisarHazardComForwarding(string caminhoArquivo)
    {
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com Forwarding");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];

            // Exibe a sequência original
            aux.ExibirSequenciaOriginal(bloco, outputBuilder);
            outputBuilder.AppendLine("\nDependências detectadas:");

            for (int j = 0; j < bloco.Count - 1; j++)
            {
                try
                {
                    var instrucaoAtual = aux.AnalisarInstrucao(bloco[j]);
                    int rdAtual = instrucaoAtual.rd;
                    bool isLoad = instrucaoAtual.assembly.StartsWith("l");

                    int limite = Math.Min(j + 6, bloco.Count);
                    for (int k = j + 1; k < limite; k++)
                    {
                        var instrucaoFutura = aux.AnalisarInstrucao(bloco[k]);
                        bool isImmediate = k == j + 1;

                        if (instrucaoFutura.rs1 == rdAtual || instrucaoFutura.rs2 == rdAtual)
                        {
                            outputBuilder.AppendLine($"  Instrução {j + 1} ({instrucaoAtual.assembly}) escreve em x{rdAtual}");
                            outputBuilder.AppendLine($"  Instrução {k + 1} ({instrucaoFutura.assembly}) lê " +
                                (instrucaoFutura.rs1 == rdAtual ? $"RS1 (x{instrucaoFutura.rs1})" : "") +
                                (instrucaoFutura.rs2 == rdAtual ? $"{(instrucaoFutura.rs1 == rdAtual ? " e " : "")}RS2 (x{instrucaoFutura.rs2})" : ""));

                            if (isLoad && isImmediate)
                            {
                                outputBuilder.AppendLine("  Tipo: Load-use hazard (não resolvido por forwarding)");
                                outputBuilder.AppendLine("  Ciclos de espera necessários: 1");
                            }
                            else
                            {
                                outputBuilder.AppendLine("  Tipo: ALU-use hazard (resolvido por forwarding)");
                                outputBuilder.AppendLine("  Ciclos de espera necessários: 0");
                            }
                            outputBuilder.AppendLine();
                        }
                    }
                }
                catch (Exception)
                {
                    outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
                }
            }
        }

        // Escreve o resultado em um arquivo
        aux.EscreverArquivo(outputBuilder.ToString(), "03-ComForwarding.txt");

        int totalInstrucoes = blocos.Sum(b => b.Count);
        int totalNops = blocos.Sum(bloco =>
        {
            var nops = 0;
            for (int j = 0; j < bloco.Count - 1; j++)
            {
                var instrucaoAtual = aux.AnalisarInstrucao(bloco[j]);
                if (instrucaoAtual.assembly.StartsWith("l") && j + 1 < bloco.Count)
                {
                    var instrucaoSeguinte = aux.AnalisarInstrucao(bloco[j + 1]);
                    if (instrucaoSeguinte.rs1 == instrucaoAtual.rd || instrucaoSeguinte.rs2 == instrucaoAtual.rd)
                    {
                        nops++; // 1 ciclo apenas para load-use hazards
                    }
                }
            }
            return nops;
        });

        aux.ExibirSobrecusto("Com Forwarding", totalInstrucoes, totalNops);
    }

    // Analisa hazards inserindo instruções NOP explicitamente
    // Adiciona NOPs para garantir a correta execução das instruções dependentes
    public void AnalisarHazardComNOP(string caminhoArquivo)
    {
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com NOPs");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        // Analisar cada bloco
        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];
            var novaSequencia = new List<(string hex, string assembly, bool isNop)>();

            for (int j = 0; j < bloco.Count; j++)
            {
                try
                {
                    var instrucaoAtual = string.Concat(bloco[j].Select(c => aux.ConverterHexParaBinario(char.ToUpper(c))));
                    var camposAtual = aux.SepararCamposInstrucao(instrucaoAtual);
                    var instrucaoAssemblyAtual = aux.IdentificarInstrucaoAssembly(camposAtual.opcode, camposAtual.funct3, camposAtual.funct7);
                    int rdAtual = Convert.ToInt32(camposAtual.rd, 2);

                    novaSequencia.Add((bloco[j], instrucaoAssemblyAtual, false));

                    if (j + 1 < bloco.Count)
                    {
                        var instrucaoSeguinte = string.Concat(bloco[j + 1].Select(c => aux.ConverterHexParaBinario(char.ToUpper(c))));
                        var camposSeguinte = aux.SepararCamposInstrucao(instrucaoSeguinte);
                        int rs1Seguinte = Convert.ToInt32(camposSeguinte.rs1, 2);
                        int rs2Seguinte = Convert.ToInt32(camposSeguinte.rs2, 2);

                        if (rs1Seguinte == rdAtual || rs2Seguinte == rdAtual)
                        {
                            bool isLoad = instrucaoAssemblyAtual.StartsWith("l");

                            if (isLoad)
                            {
                                novaSequencia.Add(("00000000", $"(NOP) -> Load {instrucaoAssemblyAtual} precisa de 1 ciclo para buscar x{rdAtual} da memória", true));
                            }
                            else
                            {
                                novaSequencia.Add(("00000000", $"(NOP) -> {instrucaoAssemblyAtual} ainda não escreveu x{rdAtual} na ALU (ciclo 1/2)", true));
                                novaSequencia.Add(("00000000", $"(NOP) -> {instrucaoAssemblyAtual} ainda não escreveu x{rdAtual} na ALU (ciclo 2/2)", true));
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
                }
            }

            // Exibe a sequência original
            outputBuilder.AppendLine("Sequência original:");
            for (int j = 0; j < bloco.Count; j++)
            {
                var (_, assembly, _, _, _) = aux.AnalisarInstrucao(bloco[j]);
                outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
            }

            // Exibe a sequência com NOPs
            outputBuilder.AppendLine("\nSequência com NOPs:");
            for (int j = 0; j < novaSequencia.Count; j++)
            {
                var (hex, assembly, isNop) = novaSequencia[j];
                if (isNop)
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
                }
                else
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly})");
                }
            }
            outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {novaSequencia.Count(x => x.isNop)}");
            outputBuilder.AppendLine();
        }

        // Calcular o sobrecusto total
        int totalInstrucoes = blocos.Sum(b => b.Count);
        int totalNops = blocos.Sum(bloco =>
        {
            // Conta NOPs necessários para cada instrução no bloco
            var nops = 0;
            for (int j = 0; j < bloco.Count - 1; j++)
            {
                var instrucaoAtual = aux.AnalisarInstrucao(bloco[j]);
                if (j + 1 < bloco.Count)
                {
                    var instrucaoSeguinte = aux.AnalisarInstrucao(bloco[j + 1]);
                    if (instrucaoSeguinte.rs1 == instrucaoAtual.rd || instrucaoSeguinte.rs2 == instrucaoAtual.rd)
                    {
                        nops += instrucaoAtual.assembly.StartsWith("l") ? 1 : 2;
                    }
                }
            }
            return nops;
        });

        aux.EscreverArquivo(outputBuilder.ToString(), "04-ComNOPs.txt");

        aux.ExibirSobrecusto("Com NOPs", totalInstrucoes, totalNops);

    }

    // Combina forwarding com inserção de NOPs
    // Usa forwarding quando possível e insere NOPs apenas quando necessário
    public void AnalisarHazardComForwardingENOP(string caminhoArquivo)
    {
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com Forwarding + NOPs");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        // Analisar cada bloco
        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];
            var novaSequencia = new List<(string hex, string assembly, bool isNop)>();

            for (int j = 0; j < bloco.Count; j++)
            {
                try
                {
                    var instrucaoAtual = string.Concat(bloco[j].Select(c => aux.ConverterHexParaBinario(char.ToUpper(c))));
                    var camposAtual = aux.SepararCamposInstrucao(instrucaoAtual);
                    var instrucaoAssemblyAtual = aux.IdentificarInstrucaoAssembly(camposAtual.opcode, camposAtual.funct3, camposAtual.funct7);
                    int rdAtual = Convert.ToInt32(camposAtual.rd, 2);

                    novaSequencia.Add((bloco[j], instrucaoAssemblyAtual, false));

                    if (j + 1 < bloco.Count)
                    {
                        var instrucaoSeguinte = string.Concat(bloco[j + 1].Select(c => aux.ConverterHexParaBinario(char.ToUpper(c))));
                        var camposSeguinte = aux.SepararCamposInstrucao(instrucaoSeguinte);
                        int rs1Seguinte = Convert.ToInt32(camposSeguinte.rs1, 2);
                        int rs2Seguinte = Convert.ToInt32(camposSeguinte.rs2, 2);

                        bool isLoad = instrucaoAssemblyAtual.StartsWith("l");

                        if (rs1Seguinte == rdAtual || rs2Seguinte == rdAtual)
                        {
                            novaSequencia.Add(("00000000", $"(NOP) -> {instrucaoAssemblyAtual} ainda não escreveu x{rdAtual} na ALU (ciclo 1/2)", true));
                            novaSequencia.Add(("00000000", $"(NOP) -> {instrucaoAssemblyAtual} ainda não escreveu x{rdAtual} na ALU (ciclo 2/2)", true));
                        }
                    }
                }
                catch (Exception)
                {
                    outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
                }
            }

            // Exibe a sequência original
            outputBuilder.AppendLine("Sequência original:");
            for (int j = 0; j < bloco.Count; j++)
            {
                var (_, assembly, _, _, _) = aux.AnalisarInstrucao(bloco[j]);
                outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
            }

            // Exibe a sequência com Forwarding + NOPs
            outputBuilder.AppendLine("\nSequência com Forwarding + NOPs:");
            for (int j = 0; j < novaSequencia.Count; j++)
            {
                var (hex, assembly, isNop) = novaSequencia[j];
                if (isNop)
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
                }
                else
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly})");
                }
            }
            outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {novaSequencia.Count(x => x.isNop)}");
            outputBuilder.AppendLine();
        }

        // Calcular e exibir sobrecusto
        int totalInstrucoes = blocos.Sum(b => b.Count);
        int totalNops = blocos.Sum(bloco =>
        {
            var nops = 0;
            for (int j = 0; j < bloco.Count - 1; j++)
            {
                var instrucaoAtual = aux.AnalisarInstrucao(bloco[j]);
                if (instrucaoAtual.assembly.StartsWith("l") && j + 1 < bloco.Count)
                {
                    var instrucaoSeguinte = aux.AnalisarInstrucao(bloco[j + 1]);
                    if (instrucaoSeguinte.rs1 == instrucaoAtual.rd || instrucaoSeguinte.rs2 == instrucaoAtual.rd)
                    {
                        nops++; // 1 ciclo apenas para load-use hazards
                    }
                }
            }
            return nops;
        });

        aux.ExibirSobrecusto("Com Forwarding + NOPs", totalInstrucoes, totalNops);
    }

    // Reordena instruções para minimizar hazards
    // Tenta reorganizar a sequência de instruções para reduzir dependências
    public void AnalisarHazardComReordenacao(string caminhoArquivo)
    {
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com Reordenação");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        // Analisar cada bloco
        int totalInstrucoes = 0;
        int totalNops = 0;
        int totalReordenadas = 0;

        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];
            totalInstrucoes += bloco.Count;

            // Lista de instruções com suas dependências
            var instrucoes = new List<(string hex, string assembly, int rd, HashSet<int> dependencias, int posOriginal)>();

            // Primeira passagem: identificar todas as instruções e suas dependências
            for (int j = 0; j < bloco.Count; j++)
            {
                try
                {
                    var (hex, assembly, rd, rs1, rs2) = aux.AnalisarInstrucao(bloco[j]);
                    instrucoes.Add((hex, assembly, rd, new HashSet<int> { rs1, rs2 }, j));
                }
                catch (Exception)
                {
                    outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
                }
            }

            // Reordenar instruções para minimizar dependências
            var reordenadas = new List<(string hex, string assembly, bool isNop, int posOriginal)>();
            var registradoresEmUso = new HashSet<int>();
            var instrucoesRestantes = new List<(string hex, string assembly, int rd, HashSet<int> dependencias, int posOriginal)>(instrucoes);

            while (instrucoesRestantes.Any())
            {
                bool encontrouInstrucaoIndependente = false;

                // Procura por uma instrução que não dependa dos registradores em uso
                for (int j = 0; j < instrucoesRestantes.Count; j++)
                {
                    var instrucao = instrucoesRestantes[j];
                    if (!instrucao.dependencias.Overlaps(registradoresEmUso))
                    {
                        if (reordenadas.Count != instrucao.posOriginal) totalReordenadas++;
                        reordenadas.Add((instrucao.hex, instrucao.assembly, false, instrucao.posOriginal));
                        registradoresEmUso.Add(instrucao.rd);
                        instrucoesRestantes.RemoveAt(j);
                        encontrouInstrucaoIndependente = true;
                        break;
                    }
                }

                // Se não encontrou instrução independente, adiciona NOP e pega a próxima da fila
                if (!encontrouInstrucaoIndependente && instrucoesRestantes.Any())
                {
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
            for (int j = 0; j < bloco.Count; j++)
            {
                var (_, assembly, _, _, _) = aux.AnalisarInstrucao(bloco[j]);
                outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
            }

            // Exibe a sequência reordenada
            outputBuilder.AppendLine("\nSequência reordenada (com NOPs quando necessário):");
            for (int j = 0; j < reordenadas.Count; j++)
            {
                var (hex, assembly, isNop, posOriginal) = reordenadas[j];
                if (isNop)
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
                }
                else
                {
                    var indicadorReordenacao = posOriginal != j ? $" -> [Movida da posição {posOriginal + 1}]" : "";
                    outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly}){indicadorReordenacao}");
                }
            }

            outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {reordenadas.Count(x => x.isNop)}");
            outputBuilder.AppendLine();
        }

        aux.EscreverArquivo(outputBuilder.ToString(), "06-ComReordenacao.txt");

        aux.ExibirSobrecusto("Com Reordenação", totalInstrucoes, totalNops);
    }

    // Combina forwarding com reordenação de instruções
    // Utiliza ambas as técnicas para otimizar o pipeline e reduzir dependências
    public void AnalisarHazardComForwardingEReordenacao(string caminhoArquivo)
    {
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com Forwarding + Reordenação");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        // Analisar cada bloco
        int totalInstrucoes = 0;
        int totalNops = 0;
        int totalReordenadas = 0;

        // Analisar cada bloco
        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];
            totalInstrucoes += bloco.Count;

            // Lista de instruções com suas dependências
            var instrucoes = new List<(string hex, string assembly, bool isLoad, int rd, HashSet<int> dependencias, int posOriginal)>();

            // Primeira passagem: identificar todas as instruções e suas dependências
            for (int j = 0; j < bloco.Count; j++)
            {
                try
                {
                    var (hex, assembly, rd, rs1, rs2) = aux.AnalisarInstrucao(bloco[j]);
                    var isLoad = assembly.StartsWith("l");
                    instrucoes.Add((hex, assembly, isLoad, rd, new HashSet<int> { rs1, rs2 }, j));
                }
                catch (Exception)
                {
                    outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
                }
            }

            // Reordenar instruções considerando forwarding
            var reordenadas = new List<(string hex, string assembly, bool isNop, int posOriginal)>();
            var registradoresEmUso = new Dictionary<int, bool>(); // rd -> isLoad
            var instrucoesRestantes = new List<(string hex, string assembly, bool isLoad, int rd, HashSet<int> dependencias, int posOriginal)>(instrucoes);

            while (instrucoesRestantes.Any())
            {
                bool encontrouInstrucaoValida = false;

                // Procura por uma instrução que possa ser executada com forwarding
                for (int j = 0; j < instrucoesRestantes.Count; j++)
                {
                    var instrucao = instrucoesRestantes[j];
                    bool temDependenciaLoad = instrucao.dependencias.Any(dep =>
                        registradoresEmUso.ContainsKey(dep) && registradoresEmUso[dep]);

                    if (!temDependenciaLoad)
                    {
                        if (reordenadas.Count != instrucao.posOriginal) totalReordenadas++;
                        reordenadas.Add((instrucao.hex, instrucao.assembly, false, instrucao.posOriginal));
                        if (registradoresEmUso.ContainsKey(instrucao.rd))
                        {
                            registradoresEmUso[instrucao.rd] = instrucao.isLoad;
                        }
                        else
                        {
                            registradoresEmUso.Add(instrucao.rd, instrucao.isLoad);
                        }
                        instrucoesRestantes.RemoveAt(j);
                        encontrouInstrucaoValida = true;
                        break;
                    }
                }

                // Se não encontrou instrução válida, adiciona NOP e a próxima instrução
                if (!encontrouInstrucaoValida && instrucoesRestantes.Any())
                {
                    var proxima = instrucoesRestantes[0];
                    if (proxima.dependencias.Any(dep => registradoresEmUso.ContainsKey(dep) && registradoresEmUso[dep]))
                    {
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
                foreach (var reg in regsParaRemover)
                {
                    registradoresEmUso.Remove(reg);
                }
            }

            // Exibe a sequência original
            outputBuilder.AppendLine("Sequência original:");
            for (int j = 0; j < bloco.Count; j++)
            {
                var (_, assembly, _, _, _) = aux.AnalisarInstrucao(bloco[j]);
                outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
            }

            // Exibe a sequência com Forwarding + Reordenação:
            outputBuilder.AppendLine("\nSequência com Forwarding + Reordenação:");
            for (int j = 0; j < reordenadas.Count; j++)
            {
                var (hex, assembly, isNop, posOriginal) = reordenadas[j];
                if (isNop)
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
                }
                else
                {
                    var indicadorReordenacao = posOriginal != j ? $" -> [Movida da posição {posOriginal + 1}]" : "";
                    outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly}){indicadorReordenacao}");
                }
            }

            outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {reordenadas.Count(x => x.isNop)}");
            outputBuilder.AppendLine();
        }

        aux.EscreverArquivo(outputBuilder.ToString(), "07-ComForwardingEReordenacao.txt");

        aux.ExibirSobrecusto("Com Forwarding + Reordenação", totalInstrucoes, totalNops);
    }

    // Analisa hazards causados por instruções de controle (branches e jumps)
    // Insere NOPs para garantir correta execução de desvios
    public void AnalisarHazardDeControle(string caminhoArquivo)
    {
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Hazards de Controle");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        // Analisar cada bloco
        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];
            var novaSequencia = new List<(string hex, string assembly, bool isNop)>();

            for (int j = 0; j < bloco.Count; j++)
            {
                try
                {
                    var (hex, assembly, _, _, _) = aux.AnalisarInstrucao(bloco[j]);
                    bool isJump = assembly.StartsWith("j");
                    bool isBranch = assembly.StartsWith("b");

                    novaSequencia.Add((hex, assembly, false));

                    if (isJump)
                    {
                        outputBuilder.AppendLine($"\nHazard de controle detectado após instrução {j + 1} ({assembly}):");
                        outputBuilder.AppendLine("  Inserindo 1 NOP para cálculo do endereço alvo\n");
                        novaSequencia.Add(("00000000", "(NOP) -> Aguardando cálculo do endereço alvo do jump", true));
                    }
                    else if (isBranch)
                    {
                        outputBuilder.AppendLine($"\nHazard de controle detectado após instrução {j + 1} ({assembly}):");
                        outputBuilder.AppendLine("  Inserindo 2 NOPs para:");
                        outputBuilder.AppendLine("  1. Aguardar avaliação da condição");
                        outputBuilder.AppendLine("  2. Calcular endereço alvo\n");

                        novaSequencia.Add(("00000000", "(NOP) -> Aguardando avaliação da condição do branch", true));
                        novaSequencia.Add(("00000000", "(NOP) -> Aguardando cálculo do endereço alvo", true));
                    }
                }
                catch (Exception)
                {
                    outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
                }
            }

            // Exibe a sequência original
            outputBuilder.AppendLine("Sequência original:");
            for (int j = 0; j < bloco.Count; j++)
            {
                var (_, assembly, _, _, _) = aux.AnalisarInstrucao(bloco[j]);
                outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
            }

            // Exibe a sequência com NOPs para controle
            outputBuilder.AppendLine("\nSequência com NOPs para hazards de controle:");
            for (int j = 0; j < novaSequencia.Count; j++)
            {
                var (hex, assembly, isNop) = novaSequencia[j];
                if (isNop)
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} {assembly}");
                }
                else
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly})");
                }
            }

            outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {novaSequencia.Count(x => x.isNop)}");
            outputBuilder.AppendLine();
        }

        aux.EscreverArquivo(outputBuilder.ToString(), "08-HazardsDeControle.txt");

        int totalInstrucoes = blocos.Sum(b => b.Count);
        int totalNops = blocos.Sum(bloco =>
        {
            var nops = 0;
            foreach (var linha in bloco)
            {
                var instrucao = aux.AnalisarInstrucao(linha);
                if (instrucao.assembly.StartsWith("j")) nops += 1;
                else if (instrucao.assembly.StartsWith("b")) nops += 2;
            }
            return nops;
        });

        aux.ExibirSobrecusto("Hazards de controle", totalInstrucoes, totalNops);
    }

    // Implementa a técnica de delayed branch
    // Tenta preencher o slot de atraso com instruções úteis ou NOPs
    public void AnalisarHazardComDelayedBranch(string caminhoArquivo)
    {
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com Delayed Branch");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        // Analisar cada bloco
        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];
            var novaSequencia = new List<(string hex, string assembly, string comentario)>();

            for (int j = 0; j < bloco.Count; j++)
            {
                try
                {
                    var (hex, assembly, rd, rs1, rs2) = aux.AnalisarInstrucao(bloco[j]);
                    bool isBranch = assembly.StartsWith("b");
                    bool isJump = assembly.StartsWith("j");

                    if (isBranch || isJump)
                    {
                        novaSequencia.Add((hex, assembly, "Instrução de desvio"));

                        // Tenta encontrar uma instrução independente nas próximas instruções
                        bool encontrouIndependente = false;
                        if (j + 1 < bloco.Count)
                        {
                            var (nextHex, nextAssembly, nextRd, nextRs1, nextRs2) = aux.AnalisarInstrucao(bloco[j + 1]);

                            // Verifica se a próxima instrução é independente do branch/jump
                            bool isDependente = nextRs1 == rd || nextRs2 == rd;

                            if (!isDependente)
                            {
                                novaSequencia.Add((nextHex, nextAssembly, "Instrução independente movida para slot de atraso"));
                                j++; // Pula a próxima instrução já que foi movida
                                encontrouIndependente = true;
                            }
                        }

                        if (!encontrouIndependente)
                        {
                            novaSequencia.Add(("00000000", "nop", "NOP inserido no slot de atraso do branch/jump"));
                        }
                    }
                    else
                    {
                        novaSequencia.Add((hex, assembly, ""));
                    }
                }
                catch (Exception)
                {
                    outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
                }
            }

            // Exibe a sequência original
            outputBuilder.AppendLine("Sequência original:");
            for (int j = 0; j < bloco.Count; j++)
            {
                var (_, assembly, _, _, _) = aux.AnalisarInstrucao(bloco[j]);
                outputBuilder.AppendLine($"  {j + 1}. {bloco[j]} ({assembly})");
            }

            // Exibe a sequência com delayed branch
            outputBuilder.AppendLine("\nSequência com Delayed Branch:");
            for (int j = 0; j < novaSequencia.Count; j++)
            {
                var (hex, assembly, comentario) = novaSequencia[j];
                if (comentario != "")
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly}) -> {comentario}");
                }
                else
                {
                    outputBuilder.AppendLine($"  {j + 1}. {hex} ({assembly})");
                }
            }

            var nopsInseridos = novaSequencia.Count(x => x.assembly == "nop");
            var instrucoesMovidas = novaSequencia.Count(x => x.comentario.Contains("movida"));

            outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {nopsInseridos}");
            outputBuilder.AppendLine($"Total de instruções reordenadas: {instrucoesMovidas}");
            outputBuilder.AppendLine();
        }

        aux.EscreverArquivo(outputBuilder.ToString(), "09-DelayedBranch.txt");

        int totalInstrucoes = blocos.Sum(b => b.Count);
        int totalNops = blocos.Sum(bloco =>
        {
            var nops = 0;
            for (int j = 0; j < bloco.Count; j++)
            {
                var instrucao = aux.AnalisarInstrucao(bloco[j]);
                if ((instrucao.assembly.StartsWith("b") || instrucao.assembly.StartsWith("j")) &&
                    (j + 1 >= bloco.Count || !aux.PodeExecutarNoSlotDeAtraso(instrucao, aux.AnalisarInstrucao(bloco[j + 1]))))
                {
                    nops++;
                }
            }
            return nops;
        });

        aux.ExibirSobrecusto("Com Delayed Branch", totalInstrucoes, totalNops);
    }

}
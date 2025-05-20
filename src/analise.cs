// Classe responsável pela análise de diferentes tipos de hazards em instruções RISC-V
public class HazardAnalysis
{
    // Instância auxiliar para funções de manipulação de instruções
    private readonly auxFunctions aux = new auxFunctions();

    // Detecta hazards do tipo Read After Write (RAW)
    // Identifica quando uma instrução tenta ler um registrador antes que seu valor seja escrito
    public void AnalisarRAWHazard(string caminhoArquivo)
    {
        // Inicializa arquivo de saída e separa blocos
        var resultado = aux.InicializarArquivo(caminhoArquivo, "RAW Hazard");
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

                    // Verificar apenas as próximas duas instruções
                    int limite = Math.Min(j + 3, bloco.Count);
                    for (int k = j + 1; k < limite; k++)
                    {
                        var instrucaoFutura = aux.AnalisarInstrucao(bloco[k]);
                        // Se a próxima instrução lê o registrador escrito pela atual, há dependência
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

        aux.EscreverArquivo(outputBuilder.ToString(), "01-RAW.txt");

        // Calcula o total de instruções e dependências para o resumo
        int totalInstrucoes = blocos.Sum(b => b.Count);
        int totalDependencias = blocos.Sum(bloco =>
        {
            var dependencias = 0;
            for (int j = 0; j < bloco.Count - 1; j++)
            {
                var instrucaoAtual = aux.AnalisarInstrucao(bloco[j]);
                // Verificar apenas a próxima instrução para o total de dependências
                if (j + 1 < bloco.Count)
                {
                    var instrucaoSeguinte = aux.AnalisarInstrucao(bloco[j + 1]);
                    if (instrucaoSeguinte.rs1 == instrucaoAtual.rd || instrucaoSeguinte.rs2 == instrucaoAtual.rd)
                    {
                        dependencias++;
                    }
                }
            }
            return dependencias;
        });

        aux.ExibirSobrecusto("RAW Hazards", totalInstrucoes, totalDependencias);
    }

    // Analisa hazards sem utilizar forwarding
    // Requer ciclos completos de espera entre instruções dependentes
    public void AnalisarHazardSemForwarding(string caminhoArquivo)
    {
        // Inicializa arquivo de saída e separa blocos
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
                        // Se a próxima instrução lê o registrador escrito pela atual, há dependência
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
                    // Se houver dependência, adiciona 2 ciclos de espera
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
    public void AnalisarHazardComForwarding(string caminhoArquivo)
    {
        // Inicializa arquivo de saída e separa blocos
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

                    // Verificar até 5 instruções à frente
                    int limite = Math.Min(j + 6, bloco.Count);
                    for (int k = j + 1; k < limite; k++)
                    {
                        var instrucaoFutura = aux.AnalisarInstrucao(bloco[k]);
                        bool isImmediate = k == j + 1;

                        // Se a próxima instrução lê o registrador escrito pela atual, há dependência
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
                // Apenas hazards do tipo load-use exigem 1 ciclo de espera; ALU-use são resolvidos por forwarding
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
        // Inicializa arquivo de saída e separa blocos
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com NOPs");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];

            var novaSequencia = aux.InserirNOPs(
                bloco,
                instrucao => {
                    int posicao = bloco.IndexOf(instrucao.hex);
                    if (posicao + 1 >= bloco.Count) return false;
                    var proximaInstrucao = aux.AnalisarInstrucao(bloco[posicao + 1]);
                    // Verifica se há dependência com a próxima instrução
                    return proximaInstrucao.rs1 == instrucao.rd || proximaInstrucao.rs2 == instrucao.rd;
                },
                instrucao => instrucao.assembly.StartsWith("l") ? 1 : 2, // 1 ciclo para load-use, 2 ciclos para outros
                instrucao => instrucao.assembly.StartsWith("l") 
                    ? $"(NOP) -> Load {instrucao.assembly} precisa de 1 ciclo para buscar x{instrucao.rd} da memória"
                    : $"(NOP) -> {instrucao.assembly} ainda não escreveu x{instrucao.rd} na ALU (ciclo {(instrucao.assembly.StartsWith("l") ? "1/1" : "1/2")})"
            );

            // Exibe a sequência original
            aux.ExibirSequenciaOriginal(bloco, outputBuilder);

            // Exibe a sequência com NOPs
            outputBuilder.AppendLine("\nSequência com NOPs:");
            for (int j = 0; j < novaSequencia.Count; j++)
            {
                var (hex, assembly, isNop) = novaSequencia[j];
                outputBuilder.AppendLine($"  {j + 1}. {hex} {(isNop ? assembly : $"({assembly})")}");
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
                    // Insere 1 NOP para load-use e 2 NOPs para outros tipos de dependência
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
        // Inicializa arquivo de saída e separa blocos
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com Forwarding + NOPs");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];

            var novaSequencia = new List<(string hex, string assembly, bool isNop)>();

            for (int j = 0; j < bloco.Count; j++)
            {
                try
                {
                    var instrucaoAtual = aux.AnalisarInstrucao(bloco[j]);
                    novaSequencia.Add((bloco[j], instrucaoAtual.assembly, false));

                    // Verifica se há dependência com a próxima instrução
                    if (j + 1 < bloco.Count)
                    {
                        var instrucaoSeguinte = aux.AnalisarInstrucao(bloco[j + 1]);
                        bool temDependencia = instrucaoSeguinte.rs1 == instrucaoAtual.rd || 
                                            instrucaoSeguinte.rs2 == instrucaoAtual.rd;
                        bool isLoad = instrucaoAtual.assembly.StartsWith("l");

                        // Insere NOP apenas se for load-use hazard
                        if (temDependencia && isLoad)
                        {
                            novaSequencia.Add(("00000000", 
                                $"(NOP) -> Load {instrucaoAtual.assembly} precisa de 1 ciclo para buscar x{instrucaoAtual.rd} da memória", 
                                true));
                        }
                    }
                }
                catch (Exception)
                {
                    outputBuilder.AppendLine($"Erro ao analisar instrução {j + 1}");
                }
            }

            // Exibe a sequência original
            aux.ExibirSequenciaOriginal(bloco, outputBuilder);

            // Exibe a sequência com Forwarding + NOPs
            outputBuilder.AppendLine("\nSequência com Forwarding + NOPs:");
            for (int j = 0; j < novaSequencia.Count; j++)
            {
                var (hex, assembly, isNop) = novaSequencia[j];
                outputBuilder.AppendLine($"  {j + 1}. {hex} {(isNop ? assembly : $"({assembly})")}");
            }

            int nopsInseridos = novaSequencia.Count(x => x.isNop);
            outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {nopsInseridos}");
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
                // Apenas hazards do tipo load-use exigem 1 ciclo de espera; ALU-use são resolvidos por forwarding
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

        aux.EscreverArquivo(outputBuilder.ToString(), "05-ComForwardingENOPs.txt");
        aux.ExibirSobrecusto("Com Forwarding + NOPs", totalInstrucoes, totalNops);
    }

    // Reordena instruções para minimizar hazards
    // Tenta reorganizar a sequência de instruções para reduzir dependências
    public void AnalisarHazardComReordenacao(string caminhoArquivo)
    {
        // Inicializa arquivo de saída e separa blocos
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com Reordenação");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        // Analisar cada bloco
        int totalInstrucoes = 0;
        int totalNops = 0;

        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];
            totalInstrucoes += bloco.Count;

            var novaSequencia = aux.InserirNOPs(
                bloco,
                instrucao => {
                    var registradoresEmUso = new HashSet<int>();
                    int posicao = bloco.IndexOf(instrucao.hex);
                    if (posicao + 1 >= bloco.Count) return false;
                    var proximaInstrucao = aux.AnalisarInstrucao(bloco[posicao + 1]);
                    // Verifica se há dependência com a próxima instrução
                    return proximaInstrucao.rs1 == instrucao.rd || proximaInstrucao.rs2 == instrucao.rd;
                },
                instrucao => 1,
                instrucao => $"(NOP) -> Aguardando x{instrucao.rd} ficar disponível"
            );

            // Atualizar o total de NOPs
            totalNops += novaSequencia.Count(x => x.isNop);

            // Exibe a sequência original
            aux.ExibirSequenciaOriginal(bloco, outputBuilder);

            // Exibe a sequência reordenada
            outputBuilder.AppendLine("\nSequência reordenada (com NOPs quando necessário):");
            for (int j = 0; j < novaSequencia.Count; j++)
            {
                var (hex, assembly, isNop) = novaSequencia[j];
                int posOriginal = isNop ? -1 : bloco.IndexOf(hex);
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

            outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {novaSequencia.Count(x => x.isNop)}");
            outputBuilder.AppendLine();
        }

        aux.EscreverArquivo(outputBuilder.ToString(), "06-ComReordenacao.txt");

        aux.ExibirSobrecusto("Com Reordenação", totalInstrucoes, totalNops);
    }

    // Combina forwarding com reordenação de instruções
    // Utiliza ambas as técnicas para otimizar o pipeline e reduzir dependências
    public void AnalisarHazardComForwardingEReordenacao(string caminhoArquivo)
    {
        // Inicializa arquivo de saída e separa blocos
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com Forwarding + Reordenação");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        // Analisar cada bloco
        int totalInstrucoes = 0;
        int totalNops = 0;

        // Analisar cada bloco
        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];
            totalInstrucoes += bloco.Count;

            var novaSequencia = aux.InserirNOPs(
                bloco,
                instrucao => {
                    int posicao = bloco.IndexOf(instrucao.hex);
                    if (posicao + 1 >= bloco.Count) return false;
                    var proximaInstrucao = aux.AnalisarInstrucao(bloco[posicao + 1]);
                    bool isLoad = instrucao.assembly.StartsWith("l");
                    // Verifica se há dependência com a próxima instrução e se é um hazard do tipo load-use
                    return (proximaInstrucao.rs1 == instrucao.rd || proximaInstrucao.rs2 == instrucao.rd) && isLoad;
                },
                instrucao => 1, // Sempre 1 NOP para load-use hazards com forwarding
                instrucao => $"(NOP) -> Aguardando load para x{instrucao.rd} ficar disponível"
            );

            // Atualizar o total de NOPs
            totalNops += novaSequencia.Count(x => x.isNop);

            // Exibe a sequência original
            aux.ExibirSequenciaOriginal(bloco, outputBuilder);

            // Exibe a sequência com Forwarding + Reordenação
            outputBuilder.AppendLine("\nSequência com Forwarding + Reordenação:");
            for (int j = 0; j < novaSequencia.Count; j++)
            {
                var (hex, assembly, isNop) = novaSequencia[j];
                int posOriginal = isNop ? -1 : bloco.IndexOf(hex);
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

            outputBuilder.AppendLine($"\nTotal de NOPs inseridos: {novaSequencia.Count(x => x.isNop)}");
            outputBuilder.AppendLine();
        }

        aux.EscreverArquivo(outputBuilder.ToString(), "07-ComForwardingEReordenacao.txt");

        aux.ExibirSobrecusto("Com Forwarding + Reordenação", totalInstrucoes, totalNops);
    }

    // Analisa hazards causados por instruções de controle (branches e jumps)
    // Insere NOPs para garantir correta execução de desvios
    public void AnalisarHazardDeControle(string caminhoArquivo)
    {
        // Inicializa arquivo de saída e separa blocos
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Hazards de Controle");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];

            var novaSequencia = aux.InserirNOPs(
                bloco,
                instrucao => instrucao.assembly.StartsWith("j") || instrucao.assembly.StartsWith("b"),
                instrucao => instrucao.assembly.StartsWith("j") ? 1 : 2, // 1 NOP para jump, 2 NOPs para branch
                instrucao => instrucao.assembly.StartsWith("j") 
                    ? "(NOP) -> Aguardando cálculo do endereço alvo do jump"
                    : instrucao.assembly.StartsWith("b") 
                        ? "(NOP) -> Aguardando avaliação da condição do branch"
                        : "(NOP) -> Aguardando cálculo do endereço alvo"
            );

            // Exibe a sequência original
            aux.ExibirSequenciaOriginal(bloco, outputBuilder);

            // Exibe a sequência com NOPs para controle
            outputBuilder.AppendLine("\nSequência com NOPs para hazards de controle:");
            for (int j = 0; j < novaSequencia.Count; j++)
            {
                var (hex, assembly, isNop) = novaSequencia[j];
                outputBuilder.AppendLine($"  {j + 1}. {hex} {(isNop ? assembly : $"({assembly})")}");
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
                // Insere 1 NOP para jump e 2 NOPs para branch
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
        // Inicializa arquivo de saída e separa blocos
        var resultado = aux.InicializarArquivo(caminhoArquivo, "Com Delayed Branch");
        if (resultado == null) return;

        var outputBuilder = resultado.Value.outputBuilder;
        var blocos = resultado.Value.blocos;

        int totalInstrucoes = 0;
        int totalNopsInseridos = 0;

        for (int i = 0; i < blocos.Count; i++)
        {
            outputBuilder.AppendLine($"\n=== Bloco {i + 1} ===");
            var bloco = blocos[i];
            var novaSequencia = new List<(string hex, string assembly, string comentario)>();
            var registradoresEmUso = new HashSet<int>();
            
            totalInstrucoes += bloco.Count;
            int nopsBloco = 0;

            for (int j = 0; j < bloco.Count; j++)
            {
                try
                {
                    var instrucaoAtual = aux.AnalisarInstrucao(bloco[j]);
                    bool isBranch = instrucaoAtual.assembly.StartsWith("b");
                    bool isJump = instrucaoAtual.assembly.StartsWith("j");

                    if (isBranch || isJump)
                    {
                        novaSequencia.Add((bloco[j], instrucaoAtual.assembly, "Instrução de desvio"));
                        registradoresEmUso.Add(instrucaoAtual.rd);

                        // Procura instrução independente para o slot de atraso
                        bool preencheuSlot = false;
                        for (int k = j + 1; k < Math.Min(j + 3, bloco.Count); k++)
                        {
                            var candidata = aux.AnalisarInstrucao(bloco[k]);
                            
                            // Verifica múltiplos tipos de dependências
                            bool temDependencia = registradoresEmUso.Contains(candidata.rs1) ||
                                               registradoresEmUso.Contains(candidata.rs2) ||
                                               candidata.assembly.StartsWith("b") ||
                                               candidata.assembly.StartsWith("j") ||
                                               candidata.assembly.StartsWith("l") ||
                                               candidata.assembly.StartsWith("s");

                            if (!temDependencia)
                            {
                                novaSequencia.Add((bloco[k], candidata.assembly, 
                                    "Instrução independente movida para slot de atraso"));
                                registradoresEmUso.Add(candidata.rd);
                                preencheuSlot = true;
                                j = k;  // Atualiza o índice
                                break;
                            }
                        }

                        if (!preencheuSlot)
                        {
                            novaSequencia.Add(("00000000", "nop", 
                                "NOP inserido no slot de atraso"));
                        }
                    }
                    else
                    {
                        novaSequencia.Add((bloco[j], instrucaoAtual.assembly, ""));
                        registradoresEmUso.Add(instrucaoAtual.rd);
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

            nopsBloco = novaSequencia.Count(x => x.assembly == "nop");
            totalNopsInseridos += nopsBloco;

            var instrucoesMovidas = novaSequencia.Count(x => x.comentario.Contains("movida"));

            outputBuilder.AppendLine($"\nTotal de NOPs inseridos no bloco: {nopsBloco}");
            outputBuilder.AppendLine($"Total de instruções reordenadas no bloco: {instrucoesMovidas}");
            outputBuilder.AppendLine();
        }

        aux.EscreverArquivo(outputBuilder.ToString(), "09-DelayedBranch.txt");
        aux.ExibirSobrecusto("Com Delayed Branch", totalInstrucoes, totalNopsInseridos);
    }
}
// Caminho do arquivo que contém as instruções em hexadecimal
string caminhoArquivo = "./hexText.txt";

var aux = new auxFunctions();
var analise = new HazardAnalysis();

// Chama as funções principais para processar o arquivo
aux.IdentificarInstrucoesBinarias(caminhoArquivo);
analise.AnalisarRAWHazard(caminhoArquivo);
analise.AnalisarHazardSemForwarding(caminhoArquivo);
analise.AnalisarHazardComForwarding(caminhoArquivo);
analise.AnalisarHazardComNOP(caminhoArquivo);
analise.AnalisarHazardComForwardingENOP(caminhoArquivo);
analise.AnalisarHazardComReordenacao(caminhoArquivo);
analise.AnalisarHazardComForwardingEReordenacao(caminhoArquivo);
analise.AnalisarHazardDeControle(caminhoArquivo);
analise.AnalisarHazardComDelayedBranch(caminhoArquivo);
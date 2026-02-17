using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BuscaLocalMonotona
{
    class Program
    {
        static Random random = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine("=== BUSCA LOCAL MONÓTONA - DISTRIBUIÇÃO DE TAREFAS ===\n");

            // Configurações do problema
            int[] maquinasConfig = { 10, 20, 50 };
            double[] rConfig = { 1.5, 2.0 };
            int replicacoes = 10;
            int maxIteracoesSemMelhora = 1000;

            // Lista para armazenar resultados
            List<string> resultados = new List<string>();
            resultados.Add("heuristica,n,m,replicacao,tempo,iteracoes,valor,parametro");

            // Executar experimentos
            foreach (int m in maquinasConfig)
            {
                foreach (double r in rConfig)
                {
                    int n = (int)(m * r);
                    Console.WriteLine($"\n--- Configuração: m={m} máquinas, n={n} tarefas ---");

                    for (int rep = 1; rep <= replicacoes; rep++)
                    {
                        Console.WriteLine($"\nReplicação {rep}/{replicacoes}");

                        // Gerar tarefas aleatórias
                        int[] tarefas = GerarTarefas(n);

                        // Executar Primeira Melhora
                        var resultPrimeiraMelhora = ExecutarBuscaLocal(
                            tarefas, m, maxIteracoesSemMelhora, true, rep);
                        
                        resultados.Add($"monotona_primeira_melhora,{n},{m},{rep}," +
                            $"{resultPrimeiraMelhora.tempo:F2},{resultPrimeiraMelhora.iteracoes}," +
                            $"{resultPrimeiraMelhora.makespan},NA");

                        Console.WriteLine($"  Primeira Melhora - Makespan: {resultPrimeiraMelhora.makespan}, " +
                            $"Iterações: {resultPrimeiraMelhora.iteracoes}, " +
                            $"Tempo: {resultPrimeiraMelhora.tempo:F2}s");

                        // Executar Melhor Melhora
                        var resultMelhorMelhora = ExecutarBuscaLocal(
                            tarefas, m, maxIteracoesSemMelhora, false, rep);
                        
                        resultados.Add($"monotona_melhor_melhora,{n},{m},{rep}," +
                            $"{resultMelhorMelhora.tempo:F2},{resultMelhorMelhora.iteracoes}," +
                            $"{resultMelhorMelhora.makespan},NA");

                        Console.WriteLine($"  Melhor Melhora   - Makespan: {resultMelhorMelhora.makespan}, " +
                            $"Iterações: {resultMelhorMelhora.iteracoes}, " +
                            $"Tempo: {resultMelhorMelhora.tempo:F2}s");
                    }
                }
            }

            // Salvar resultados
            string nomeArquivo = "resultados_busca_monotona.csv";
            File.WriteAllLines(nomeArquivo, resultados);
            Console.WriteLine($"\n\n=== Resultados salvos em: {nomeArquivo} ===");

            // Exibir estatísticas resumidas
            ExibirEstatisticas(resultados);

            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        static int[] GerarTarefas(int n)
        {
            int[] tarefas = new int[n];
            for (int i = 0; i < n; i++)
            {
                tarefas[i] = random.Next(1, 101); // Tempo entre 1 e 100
            }
            return tarefas;
        }

        static (int makespan, int iteracoes, double tempo) ExecutarBuscaLocal(
            int[] tarefas, int numMaquinas, int maxIteracoesSemMelhora, 
            bool primeiraMelhora, int semente)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Gerar solução inicial aleatória
            int[] solucao = GerarSolucaoInicial(tarefas.Length, numMaquinas, semente);
            int makespanAtual = CalcularMakespan(tarefas, solucao, numMaquinas);

            int iteracoes = 0;
            int iteracoesSemMelhora = 0;

            while (iteracoesSemMelhora < maxIteracoesSemMelhora)
            {
                iteracoes++;
                bool melhorou = false;

                if (primeiraMelhora)
                {
                    // Primeira Melhora: aceita a primeira solução vizinha que melhora
                    melhorou = BuscaPrimeiraMelhora(tarefas, solucao, numMaquinas, 
                        ref makespanAtual);
                }
                else
                {
                    // Melhor Melhora: avalia todos os vizinhos e escolhe o melhor
                    melhorou = BuscaMelhorMelhora(tarefas, solucao, numMaquinas, 
                        ref makespanAtual);
                }

                if (melhorou)
                {
                    iteracoesSemMelhora = 0;
                }
                else
                {
                    iteracoesSemMelhora++;
                }
            }

            sw.Stop();
            return (makespanAtual, iteracoes, sw.Elapsed.TotalSeconds);
        }

        static int[] GerarSolucaoInicial(int numTarefas, int numMaquinas, int semente)
        {
            Random rnd = new Random(semente * 1000);
            int[] solucao = new int[numTarefas];
            for (int i = 0; i < numTarefas; i++)
            {
                solucao[i] = rnd.Next(numMaquinas);
            }
            return solucao;
        }

        static int CalcularMakespan(int[] tarefas, int[] solucao, int numMaquinas)
        {
            int[] cargaMaquinas = new int[numMaquinas];
            
            for (int i = 0; i < tarefas.Length; i++)
            {
                cargaMaquinas[solucao[i]] += tarefas[i];
            }

            return cargaMaquinas.Max();
        }

        static bool BuscaPrimeiraMelhora(int[] tarefas, int[] solucao, 
            int numMaquinas, ref int makespanAtual)
        {
            // Tenta mover cada tarefa para outra máquina
            for (int i = 0; i < tarefas.Length; i++)
            {
                int maquinaOriginal = solucao[i];

                for (int novaMaquina = 0; novaMaquina < numMaquinas; novaMaquina++)
                {
                    if (novaMaquina == maquinaOriginal)
                        continue;

                    // Testa o movimento
                    solucao[i] = novaMaquina;
                    int novoMakespan = CalcularMakespan(tarefas, solucao, numMaquinas);

                    if (novoMakespan < makespanAtual)
                    {
                        // Aceita a primeira melhora encontrada
                        makespanAtual = novoMakespan;
                        return true;
                    }

                    // Desfaz o movimento
                    solucao[i] = maquinaOriginal;
                }
            }

            return false;
        }

        static bool BuscaMelhorMelhora(int[] tarefas, int[] solucao, 
            int numMaquinas, ref int makespanAtual)
        {
            int melhorMakespan = makespanAtual;
            int melhorTarefa = -1;
            int melhorMaquina = -1;

            // Avalia todos os movimentos possíveis
            for (int i = 0; i < tarefas.Length; i++)
            {
                int maquinaOriginal = solucao[i];

                for (int novaMaquina = 0; novaMaquina < numMaquinas; novaMaquina++)
                {
                    if (novaMaquina == maquinaOriginal)
                        continue;

                    // Testa o movimento
                    solucao[i] = novaMaquina;
                    int novoMakespan = CalcularMakespan(tarefas, solucao, numMaquinas);

                    if (novoMakespan < melhorMakespan)
                    {
                        melhorMakespan = novoMakespan;
                        melhorTarefa = i;
                        melhorMaquina = novaMaquina;
                    }

                    // Desfaz o movimento
                    solucao[i] = maquinaOriginal;
                }
            }

            // Aplica o melhor movimento encontrado
            if (melhorTarefa != -1)
            {
                solucao[melhorTarefa] = melhorMaquina;
                makespanAtual = melhorMakespan;
                return true;
            }

            return false;
        }

        static void ExibirEstatisticas(List<string> resultados)
        {
            Console.WriteLine("\n\n=== ESTATÍSTICAS RESUMIDAS ===\n");

            var dados = resultados.Skip(1).Select(linha =>
            {
                var partes = linha.Split(',');
                return new
                {
                    Heuristica = partes[0],
                    N = int.Parse(partes[1]),
                    M = int.Parse(partes[2]),
                    Tempo = double.Parse(partes[4]),
                    Iteracoes = int.Parse(partes[5]),
                    Makespan = int.Parse(partes[6])
                };
            }).ToList();

            // Agrupar por heurística
            var grupos = dados.GroupBy(d => d.Heuristica);

            foreach (var grupo in grupos)
            {
                Console.WriteLine($"\n{grupo.Key}:");
                Console.WriteLine($"  Makespan médio: {grupo.Average(x => x.Makespan):F2}");
                Console.WriteLine($"  Makespan mínimo: {grupo.Min(x => x.Makespan)}");
                Console.WriteLine($"  Makespan máximo: {grupo.Max(x => x.Makespan)}");
                Console.WriteLine($"  Iterações médias: {grupo.Average(x => x.Iteracoes):F2}");
                Console.WriteLine($"  Tempo médio: {grupo.Average(x => x.Tempo):F4}s");
            }

            // Comparação por configuração
            Console.WriteLine("\n\n=== COMPARAÇÃO POR CONFIGURAÇÃO (m máquinas) ===\n");
            
            var configuracoes = dados.GroupBy(d => new { d.M, d.N });
            
            foreach (var config in configuracoes.OrderBy(c => c.Key.M).ThenBy(c => c.Key.N))
            {
                Console.WriteLine($"\nm={config.Key.M}, n={config.Key.N}:");
                
                var porHeuristica = config.GroupBy(d => d.Heuristica);
                foreach (var h in porHeuristica)
                {
                    Console.WriteLine($"  {h.Key}:");
                    Console.WriteLine($"    Makespan médio: {h.Average(x => x.Makespan):F2}");
                    Console.WriteLine($"    Tempo médio: {h.Average(x => x.Tempo):F4}s");
                }
            }
        }
    }
}
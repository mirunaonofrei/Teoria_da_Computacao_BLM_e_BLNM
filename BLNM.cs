using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TemperaSimulada
{
    class Program
    {
        static Random random = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine("=== TÊMPERA SIMULADA - DISTRIBUIÇÃO DE TAREFAS ===\n");

            // Configurações do problema
            int[] maquinasConfig = { 10, 20, 50 };
            double[] rConfig = { 1.5, 2.0 };
            double[] alphaConfig = { 0.8, 0.85, 0.9, 0.95, 0.99 };
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

                    foreach (double alpha in alphaConfig)
                    {
                        Console.WriteLine($"\n  Parâmetro alpha={alpha}");

                        for (int rep = 1; rep <= replicacoes; rep++)
                        {
                            // Gerar tarefas aleatórias
                            int[] tarefas = GerarTarefas(n);

                            // Executar Têmpera Simulada
                            var resultado = ExecutarTemperaSimulada(
                                tarefas, m, maxIteracoesSemMelhora, alpha, rep);

                            resultados.Add($"tempera_simulada,{n},{m},{rep}," +
                                $"{resultado.tempo:F2},{resultado.iteracoes}," +
                                $"{resultado.makespan},{alpha}");

                            Console.WriteLine($"    Rep {rep}: Makespan={resultado.makespan}, " +
                                $"Iter={resultado.iteracoes}, Tempo={resultado.tempo:F2}s");
                        }
                    }
                }
            }

            // Salvar resultados
            string nomeArquivo = "resultados_tempera_simulada.csv";
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

        static (int makespan, int iteracoes, double tempo) ExecutarTemperaSimulada(
            int[] tarefas, int numMaquinas, int maxIteracoesSemMelhora,
            double alpha, int semente)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Gerar solução inicial aleatória
            int[] solucaoAtual = GerarSolucaoInicial(tarefas.Length, numMaquinas, semente);
            int makespanAtual = CalcularMakespan(tarefas, solucaoAtual, numMaquinas);

            // Melhor solução encontrada
            int[] melhorSolucao = (int[])solucaoAtual.Clone();
            int melhorMakespan = makespanAtual;

            // Temperatura inicial: proporcional ao makespan inicial
            double temperaturaInicial = makespanAtual * 0.5;
            double temperatura = temperaturaInicial;

            // Temperatura mínima
            double temperaturaMinima = 0.01;

            int iteracoes = 0;
            int iteracoesSemMelhora = 0;

            Random rnd = new Random(semente * 1000 + (int)(alpha * 100));

            while (iteracoesSemMelhora < maxIteracoesSemMelhora && temperatura > temperaturaMinima)
            {
                iteracoes++;

                // Gerar solução vizinha: mover uma tarefa aleatória para outra máquina
                int[] solucaoVizinha = (int[])solucaoAtual.Clone();
                int tarefaAleatoria = rnd.Next(tarefas.Length);
                int maquinaOriginal = solucaoVizinha[tarefaAleatoria];
                int novaMaquina;

                do
                {
                    novaMaquina = rnd.Next(numMaquinas);
                } while (novaMaquina == maquinaOriginal);

                solucaoVizinha[tarefaAleatoria] = novaMaquina;
                int makespanVizinho = CalcularMakespan(tarefas, solucaoVizinha, numMaquinas);

                // Calcular diferença de custo (delta)
                int delta = makespanVizinho - makespanAtual;

                // Critério de aceitação
                bool aceitar = false;

                if (delta < 0)
                {
                    // Solução vizinha é melhor: sempre aceita
                    aceitar = true;
                }
                else
                {
                    // Solução vizinha é pior: aceita com probabilidade e^(-delta/T)
                    double probabilidade = Math.Exp(-delta / temperatura);
                    double valorAleatorio = rnd.NextDouble();

                    if (valorAleatorio < probabilidade)
                    {
                        aceitar = true;
                    }
                }

                if (aceitar)
                {
                    // Aceita a solução vizinha
                    solucaoAtual = solucaoVizinha;
                    makespanAtual = makespanVizinho;

                    // Atualiza a melhor solução encontrada
                    if (makespanVizinho < melhorMakespan)
                    {
                        melhorSolucao = (int[])solucaoVizinha.Clone();
                        melhorMakespan = makespanVizinho;
                        iteracoesSemMelhora = 0;
                    }
                    else
                    {
                        iteracoesSemMelhora++;
                    }
                }
                else
                {
                    iteracoesSemMelhora++;
                }

                // Resfriamento: t = t * alpha
                temperatura = temperatura * alpha;
            }

            sw.Stop();
            return (melhorMakespan, iteracoes, sw.Elapsed.TotalSeconds);
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
                    Makespan = int.Parse(partes[6]),
                    Alpha = double.Parse(partes[7])
                };
            }).ToList();

            // Estatísticas gerais
            Console.WriteLine("ESTATÍSTICAS GERAIS:");
            Console.WriteLine($"  Makespan médio: {dados.Average(x => x.Makespan):F2}");
            Console.WriteLine($"  Makespan mínimo: {dados.Min(x => x.Makespan)}");
            Console.WriteLine($"  Makespan máximo: {dados.Max(x => x.Makespan)}");
            Console.WriteLine($"  Iterações médias: {dados.Average(x => x.Iteracoes):F2}");
            Console.WriteLine($"  Tempo médio: {dados.Average(x => x.Tempo):F4}s");

            // Análise por parâmetro alpha
            Console.WriteLine("\n\n=== ANÁLISE POR PARÂMETRO ALPHA ===\n");

            var porAlpha = dados.GroupBy(d => d.Alpha).OrderBy(g => g.Key);

            Console.WriteLine($"{"Alpha",-8} {"Makespan Médio",-16} {"Makespan Min",-14} {"Tempo Médio",-14} {"Iterações Médias",-18}");
            Console.WriteLine(new string('-', 75));

            foreach (var grupo in porAlpha)
            {
                Console.WriteLine($"{grupo.Key,-8:F2} {grupo.Average(x => x.Makespan),-16:F2} " +
                    $"{grupo.Min(x => x.Makespan),-14} {grupo.Average(x => x.Tempo),-14:F4} " +
                    $"{grupo.Average(x => x.Iteracoes),-18:F2}");
            }

            // Melhor parâmetro por qualidade
            var melhorAlphaQualidade = porAlpha.OrderBy(g => g.Average(x => x.Makespan)).First();
            Console.WriteLine($"\n✓ Melhor alpha por QUALIDADE: {melhorAlphaQualidade.Key:F2} " +
                $"(makespan médio: {melhorAlphaQualidade.Average(x => x.Makespan):F2})");

            // Melhor parâmetro por velocidade
            var melhorAlphaVelocidade = porAlpha.OrderBy(g => g.Average(x => x.Tempo)).First();
            Console.WriteLine($"✓ Melhor alpha por VELOCIDADE: {melhorAlphaVelocidade.Key:F2} " +
                $"(tempo médio: {melhorAlphaVelocidade.Average(x => x.Tempo):F4}s)");

            // Análise por configuração (m, n)
            Console.WriteLine("\n\n=== ANÁLISE POR CONFIGURAÇÃO ===\n");

            var configuracoes = dados.GroupBy(d => new { d.M, d.N });

            foreach (var config in configuracoes.OrderBy(c => c.Key.M).ThenBy(c => c.Key.N))
            {
                Console.WriteLine($"\nm={config.Key.M}, n={config.Key.N}:");

                var porAlphaConfig = config.GroupBy(d => d.Alpha).OrderBy(g => g.Key);

                Console.WriteLine($"  {"Alpha",-8} {"Makespan Médio",-16} {"Tempo Médio",-14}");
                Console.WriteLine("  " + new string('-', 40));

                foreach (var grupo in porAlphaConfig)
                {
                    Console.WriteLine($"  {grupo.Key,-8:F2} {grupo.Average(x => x.Makespan),-16:F2} " +
                        $"{grupo.Average(x => x.Tempo),-14:F4}");
                }
            }

            // Análise de trade-off qualidade vs velocidade
            Console.WriteLine("\n\n=== TRADE-OFF QUALIDADE vs VELOCIDADE ===\n");

            var alphaOrdenadoQualidade = porAlpha.OrderBy(g => g.Average(x => x.Makespan)).ToList();
            var alphaOrdenadoVelocidade = porAlpha.OrderBy(g => g.Average(x => x.Tempo)).ToList();

            Console.WriteLine("Ranking por QUALIDADE (menor makespan):");
            for (int i = 0; i < alphaOrdenadoQualidade.Count; i++)
            {
                var grupo = alphaOrdenadoQualidade[i];
                Console.WriteLine($"  {i + 1}. Alpha={grupo.Key:F2} - Makespan: {grupo.Average(x => x.Makespan):F2}");
            }

            Console.WriteLine("\nRanking por VELOCIDADE (menor tempo):");
            for (int i = 0; i < alphaOrdenadoVelocidade.Count; i++)
            {
                var grupo = alphaOrdenadoVelocidade[i];
                Console.WriteLine($"  {i + 1}. Alpha={grupo.Key:F2} - Tempo: {grupo.Average(x => x.Tempo):F4}s");
            }

            // Recomendação
            Console.WriteLine("\n\n=== RECOMENDAÇÃO ===\n");

            var melhorBalanco = porAlpha
                .Select(g => new
                {
                    Alpha = g.Key,
                    Makespan = g.Average(x => x.Makespan),
                    Tempo = g.Average(x => x.Tempo),
                    Score = (g.Average(x => x.Makespan) / dados.Min(x => x.Makespan)) +
                            (g.Average(x => x.Tempo) / dados.Min(x => x.Tempo))
                })
                .OrderBy(x => x.Score)
                .First();

            Console.WriteLine($"✓ Melhor BALANÇO qualidade/velocidade: Alpha={melhorBalanco.Alpha:F2}");
            Console.WriteLine($"  - Makespan médio: {melhorBalanco.Makespan:F2}");
            Console.WriteLine($"  - Tempo médio: {melhorBalanco.Tempo:F4}s");
        }
    }
}
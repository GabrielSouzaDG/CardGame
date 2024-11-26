﻿using Domain.Entities;
using Domain.Factories;
using Domain.Interfaces.Iterator;
using Domain.Resources;
using Domain.Strategies;
using System.Text;

namespace Presentation.Views
{
    public static class Mesa
    {
        public static void Main()
        {
            Configurar();

            var jogadores = ArrangeJogadores();

            do
            {
                var (baralho, iterator) = ArrangeMesa();

                jogadores[0].SetStrategy(new DealerStrategy());
                jogadores[1].SetStrategy(new ComputadorStrategy());
                jogadores[2].SetStrategy(new ComputadorStrategy());
                jogadores[3].SetStrategy(new HumanoStrategy());

                var apostas = RealizarApostas(jogadores);

                jogadores.ForEach(j => j.Cartas.Distribuir(iterator, 2));

                JogarRodadas(jogadores, iterator);

                JogarRodadasDealer(jogadores, iterator);

                DefinirVencedores(jogadores, CalcularPontuacoes(jogadores), apostas);

                jogadores.ForEach(jogador => jogador.Resetar());

            } while (jogadores[3].Pontuacao > 0);

            Console.Clear();
            Console.WriteLine("Fim de jogo!");
        }

        private static void Configurar()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        private static (Baralho baralho, IBaralhoIterator iterator) ArrangeMesa()
        {
            var baralho = new Baralho();
            return (baralho, baralho.CriarIterator());
        }

        private static List<Jogador> ArrangeJogadores()
        {
            var factory = new JogadorFactory();
            return
            [
                factory.CriarJogador("dealer", Enums.TipoJogador.Dealer),
                factory.CriarJogador("computador 1", Enums.TipoJogador.Computador),
                factory.CriarJogador("computador 2", Enums.TipoJogador.Computador),
                factory.CriarJogador("jogador", Enums.TipoJogador.Pessoa),
            ];
        }

        private static Dictionary<string, int> RealizarApostas(List<Jogador> jogadores)
        {
            var apostas = jogadores.ToDictionary(
                jogador => jogador.Nome,
                jogador => jogador.Apostar()
            );

            jogadores.ForEach(jogador => jogador.Pontuacao -= apostas[jogador.Nome]);

            return apostas;
        }

        private static void JogarRodadas(List<Jogador> jogadores, IBaralhoIterator iterator)
        {
            bool continua;

            do
            {
                var jogadas = new List<bool>();

                foreach (var jogador in jogadores.Skip(1).Reverse())
                {
                    Console.Clear();
                    ExibirCartas(jogadores);

                    jogadas.Add(RealizarAcao(jogador, iterator));
                }

                continua = !jogadas.All(a => a);

            } while (continua);
        }

        private static void JogarRodadasDealer(List<Jogador> jogadores, IBaralhoIterator iterator)
        {
            var dealer = jogadores[0];
            bool continua;
            do
            {
                Console.Clear();
                RevelarCartas(jogadores);

                continua = !RealizarAcao(dealer, iterator);

                Thread.Sleep(1000);

            } while (continua);
        }

        private static void ExibirCartas(List<Jogador> jogadores)
        {
            ExibirCartasComFormato(jogadores[0], ocultarSegundaCarta: true);
            jogadores.Skip(1).ToList().ForEach(j => ExibirCartasComFormato(j));
        }

        private static void RevelarCartas(List<Jogador> jogadores)
        {
            jogadores.ForEach(j => ExibirCartasComFormato(j));
        }

        private static Dictionary<string, int> CalcularPontuacoes(List<Jogador> jogadores) =>
            jogadores.ToDictionary(j => j.Nome, j => Carta.GetValorTotal(j.Cartas));

        private static void DefinirVencedores(List<Jogador> jogadores, Dictionary<string, int> pontuacoes, Dictionary<string, int> apostas)
        {
            var dealer = jogadores[0];
            var pontuacaoDealer = pontuacoes[dealer.Nome];

            Thread.Sleep(700);
            Console.Write(".");
            Thread.Sleep(700);
            Console.Write(".");
            Thread.Sleep(700);
            Console.Write(".");
            Thread.Sleep(700);
            Console.Clear();
            Console.WriteLine($"Pontuação do Dealer: {pontuacaoDealer}");
            Console.WriteLine();

            foreach (var jogador in jogadores.Skip(1))
            {
                Thread.Sleep(2000);
                var pontuacaoJogador = pontuacoes[jogador.Nome];
                Console.WriteLine($"Jogador: {jogador.Nome} - Pontuação: {pontuacaoJogador}");
                Thread.Sleep(700);
                if (pontuacaoJogador > 21)
                {
                    Console.WriteLine($"{jogador.Nome} perdeu (estourou).");
                    jogador.Vitorias--;
                }
                else if (pontuacaoDealer > 21 || pontuacaoJogador > pontuacaoDealer)
                {
                    Console.WriteLine($"{jogador.Nome} ganhou!");
                    jogador.Pontuacao += apostas[jogador.Nome] * 2;
                    jogador.Vitorias++;
                }
                else if (pontuacaoJogador == pontuacaoDealer)
                {
                    Console.WriteLine($"{jogador.Nome} empatou e recuperou sua aposta.");
                    jogador.Pontuacao += apostas[jogador.Nome];
                }
                else
                {
                    Console.WriteLine($"{jogador.Nome} perdeu.");
                    jogador.Vitorias--;
                }

                Console.WriteLine();
            }

            Thread.Sleep(700);
            Console.Write(".");
            Thread.Sleep(700);
            Console.Write(".");
            Thread.Sleep(700);
            Console.Write(".");
            Thread.Sleep(1500);
            Console.Clear();
        }

        private static void ExibirCartasComFormato(Jogador jogador, bool ocultarSegundaCarta = false)
        {
            var linhas = ocultarSegundaCarta
                ?
                [
            jogador.Cartas[0].ObterLinhasCarta(),
            jogador.Cartas[0].ObterLinhasCartaVirada()
                ]
                : jogador.Cartas.Select(c => c.ObterLinhasCarta()).ToList();

            Console.WriteLine($"Cartas de {jogador.Nome} ({jogador.Pontuacao} fichas):");
            for (int i = 0; i < 7; i++)
                Console.WriteLine(string.Join(" ", linhas.Select(l => l[i])));
        }

        private static bool RealizarAcao(Jogador jogador, IBaralhoIterator iterator)
        {
            var acao = jogador.Jogar();
            if (acao == Enums.Acao.Puxar)
                jogador.Cartas.Add(PuxarCarta(iterator));
            else if (acao == Enums.Acao.Manter)
                jogador.SetStrategy(new JogadorManterStrategy());
            else if (acao == Enums.Acao.DoubleDown)
                jogador.SetStrategy(new DoubleDownStrategy());
            return acao == Enums.Acao.Manter || acao == Enums.Acao.DoubleDown;
        }

        private static Carta PuxarCarta(IBaralhoIterator iterator)
        {
            return iterator.Next();
        }

        private static void Distribuir(this List<Carta> cartas, IBaralhoIterator iterator, int quantidade)
        {
            for (int i = 0; i < quantidade; i++)
                cartas.Add(iterator.Next());
        }
    }
}
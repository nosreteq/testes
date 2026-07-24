using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Testes.Ui
{
    /// <summary>
    /// Testes unitários de PosicionaBotoes().
    ///
    /// Estratégia:
    /// - O SUT é instanciado via FormatterServices.GetUninitializedObject para NÃO executar
    ///   o construtor (evita InitializeComponent / carga de BAML fora do host WPF).
    /// - Os campos gerados pelo XAML (internal) e o campo privado _tokenValidoRetransmissao
    ///   são preenchidos por reflection.
    /// - Toda criação de controle WPF exige apartamento STA; MSTest roda MTA por padrão,
    ///   então cada teste executa em thread STA dedicada (ExecutarSta).
    /// </summary>
    [TestClass]
    public class PosicionaBotoesTests
    {
        // TODO: substituir pelo tipo real da partial class que contém PosicionaBotoes().
        // Ex.: typeof(MinhaEmpresa.Telas.TelaLimiteRetransmissao)
        private static readonly Type TipoSut = typeof(SeuNamespace.SuaClasseComPosicionaBotoes);

        private const string NomeMetodo     = "PosicionaBotoes";
        private const string CampoToken     = "_tokenValidoRetransmissao";
        private const string CampoBtnToken  = "btnTokenLimparTabelaLimiteRetrans";
        private const string CampoBtnLimpar = "btnLimparTabelaLimiteRetrans";

        private static readonly Thickness MargemPadrao   = new Thickness(0, 5, 154, 0);
        private static readonly Thickness MargemDeslocada = new Thickness(0, 5, 308, 0);

        // Sentinela: garante que o método de fato SOBRESCREVE a margem, e não
        // apenas "coincide" com o valor default do controle.
        private static readonly Thickness MargemSentinela = new Thickness(-1, -1, -1, -1);

        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // ---------------------------------------------------------------
        // Testes
        // ---------------------------------------------------------------

        [TestMethod]
        public void PosicionaBotoes_TokenInvalido_AmbosBotoesRecebemMargem154()
        {
            ExecutarSta(() =>
            {
                var ctx = CriarSut(tokenValido: false);

                InvocarPosicionaBotoes(ctx.Sut);

                Assert.AreEqual(MargemPadrao, ctx.BtnToken.Margin,
                    "btnTokenLimparTabelaLimiteRetrans deveria ter Margin (0,5,154,0) com token inválido.");
                Assert.AreEqual(MargemPadrao, ctx.BtnLimpar.Margin,
                    "btnLimparTabelaLimiteRetrans deveria ter Margin (0,5,154,0) com token inválido.");
            });
        }

        [TestMethod]
        public void PosicionaBotoes_TokenValido_BtnLimparRecebeMargem308()
        {
            ExecutarSta(() =>
            {
                var ctx = CriarSut(tokenValido: true);

                InvocarPosicionaBotoes(ctx.Sut);

                Assert.AreEqual(MargemPadrao, ctx.BtnToken.Margin,
                    "btnTokenLimparTabelaLimiteRetrans deveria manter Margin (0,5,154,0) com token válido (comportamento atual do código).");
                Assert.AreEqual(MargemDeslocada, ctx.BtnLimpar.Margin,
                    "btnLimparTabelaLimiteRetrans deveria ter Margin (0,5,308,0) com token válido.");
            });
        }

        [TestMethod]
        public void PosicionaBotoes_SobrescreveMargensPreExistentes()
        {
            ExecutarSta(() =>
            {
                var ctx = CriarSut(tokenValido: false);
                ctx.BtnToken.Margin  = MargemSentinela;
                ctx.BtnLimpar.Margin = MargemSentinela;

                InvocarPosicionaBotoes(ctx.Sut);

                Assert.AreNotEqual(MargemSentinela, ctx.BtnToken.Margin,
                    "O método deveria sobrescrever a margem pré-existente do btnToken.");
                Assert.AreNotEqual(MargemSentinela, ctx.BtnLimpar.Margin,
                    "O método deveria sobrescrever a margem pré-existente do btnLimpar.");
            });
        }

        // ---------------------------------------------------------------
        // Infraestrutura
        // ---------------------------------------------------------------

        private sealed class ContextoSut
        {
            public object Sut;
            public Button BtnToken;
            public Button BtnLimpar;
        }

        private static ContextoSut CriarSut(bool tokenValido)
        {
            // Não executa o construtor: dispensa InitializeComponent e recursos XAML.
            var sut = FormatterServices.GetUninitializedObject(TipoSut);

            var btnToken  = new Button();
            var btnLimpar = new Button();

            DefinirCampo(sut, CampoBtnToken, btnToken);
            DefinirCampo(sut, CampoBtnLimpar, btnLimpar);
            DefinirCampo(sut, CampoToken, tokenValido);

            return new ContextoSut { Sut = sut, BtnToken = btnToken, BtnLimpar = btnLimpar };
        }

        private static void InvocarPosicionaBotoes(object sut)
        {
            var metodo = ObterMetodo(TipoSut, NomeMetodo);
            Assert.IsNotNull(metodo,
                $"Método '{NomeMetodo}' não encontrado em {TipoSut.FullName}. " +
                "Verifique nome e se o assembly referenciado é o correto.");

            try
            {
                metodo.Invoke(sut, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Repropaga a exceção real preservando o stack trace.
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        private static void DefinirCampo(object alvo, string nome, object valor)
        {
            var campo = ObterCampo(alvo.GetType(), nome);
            Assert.IsNotNull(campo,
                $"Campo '{nome}' não encontrado em {alvo.GetType().FullName} nem em suas bases. " +
                "Campos x:Name do XAML são gerados como 'internal' — reflection cobre isso, " +
                "mas confirme o nome exato no .g.cs / XAML.");
            campo.SetValue(alvo, valor);
        }

        private static FieldInfo ObterCampo(Type tipo, string nome)
        {
            for (var t = tipo; t != null; t = t.BaseType)
            {
                var campo = t.GetField(nome, Flags | BindingFlags.DeclaredOnly);
                if (campo != null) return campo;
            }
            return null;
        }

        private static MethodInfo ObterMetodo(Type tipo, string nome)
        {
            for (var t = tipo; t != null; t = t.BaseType)
            {
                var metodo = t.GetMethod(nome, Flags | BindingFlags.DeclaredOnly,
                    null, Type.EmptyTypes, null);
                if (metodo != null) return metodo;
            }
            return null;
        }

        /// <summary>
        /// Executa a ação em thread STA (obrigatório para instanciar controles WPF).
        /// Falhas de assert/exceções são repropagadas na thread do MSTest.
        /// </summary>
        private static void ExecutarSta(Action acao)
        {
            Exception falha = null;

            var thread = new Thread(() =>
            {
                try { acao(); }
                catch (Exception ex) { falha = ex; }
            })
            {
                IsBackground = true
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (falha != null)
                ExceptionDispatchInfo.Capture(falha).Throw();
        }
    }
}

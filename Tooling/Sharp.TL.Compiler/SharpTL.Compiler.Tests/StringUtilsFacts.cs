// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringUtilsFacts.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using FluentAssertions;
using NUnit.Framework;
using SharpTL.Compiler.Utils;

namespace SharpTL.Compiler.Tests
{
    [TestFixture]
    public class StringUtilsFacts
    {
        [TestCase("resPQ", "ResPQ")]
        [TestCase("rpc_answer_unknown", "RpcAnswerUnknown")]
        [TestCase("p_q_inner_data", "PQInnerData")]
        [TestCase("server_DH_params_fail", "ServerDHParamsFail")]
        [TestCase("Set_client_DH_params_answer", "SetClientDHParamsAnswer")]
        [TestCase("Server_DH_Params", "ServerDHParams")]
        public void Should_convert_string_to_pascal_case(string text, string expectedText)
        {
            string actualText = text.ToConventionalCase(Case.PascalCase);
            actualText.Should().Be(expectedText);
        }

        [TestCase("resPQ", "resPQ")]
        [TestCase("rpc_answer_unknown", "rpcAnswerUnknown")]
        [TestCase("p_q_inner_data", "pQInnerData")]
        [TestCase("server_DH_params_fail", "serverDHParamsFail")]
        [TestCase("Set_client_DH_params_answer", "setClientDHParamsAnswer")]
        [TestCase("Server_DH_Params", "serverDHParams")]
        public void Should_convert_string_to_camel_case(string text, string expectedText)
        {
            string actualText = text.ToConventionalCase(Case.CamelCase);
            actualText.Should().Be(expectedText);
        }
    }
}

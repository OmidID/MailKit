﻿//
// SaslMechanismTests.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Net;
using System.Text;

using NUnit.Framework;

using MailKit.Security;
using MailKit.Security.Ntlm;

namespace UnitTests.Security {
	[TestFixture]
	public class SaslMechanismTests
	{
		[Test]
		public void TestCramMd5ExampleFromRfc2195 ()
		{
			const string serverToken = "<1896.697170952@postoffice.example.net>";
			const string expected = "joe 3dbc88f0624776a737b39093f6eb6427";
			var credentials = new NetworkCredential ("joe", "tanstaaftanstaaf");
			var uri = new Uri ("imap://imap.gmail.com");
			var sasl = new SaslMechanismCramMd5 (uri, credentials);

			var token = Encoding.ASCII.GetBytes (serverToken);
			var challenge = sasl.Challenge (Convert.ToBase64String (token));
			var decoded = Convert.FromBase64String (challenge);
			var result = Encoding.ASCII.GetString (decoded);

			Assert.AreEqual (expected, result, "CRAM-MD5 challenge response does not match the expected string.");
			Assert.IsTrue (sasl.IsAuthenticated, "CRAM-MD5 should be authenticated.");
		}

		[Test]
		public void TestDigestMd5ExampleFromRfc2831 ()
		{
			const string serverToken1 = "realm=\"elwood.innosoft.com\",nonce=\"OA6MG9tEQGm2hh\",qop=\"auth\",algorithm=md5-sess,charset=utf-8";
			const string expected1 = "username=\"chris\",realm=\"elwood.innosoft.com\",nonce=\"OA6MG9tEQGm2hh\",cnonce=\"OA6MHXh6VqTrRk\",nc=00000001,qop=\"auth\",digest-uri=\"imap/elwood.innosoft.com\",response=\"d388dad90d4bbd760a152321f2143af7\",charset=\"utf-8\",algorithm=\"md5-sess\"";
			const string serverToken2 = "rspauth=ea40f60335c427b5527b84dbabcdfffd";
			var credentials = new NetworkCredential ("chris", "secret");
			var uri = new Uri ("imap://elwood.innosoft.com");
			var sasl = new SaslMechanismDigestMd5 (uri, credentials, "OA6MHXh6VqTrRk");

			var token = Encoding.ASCII.GetBytes (serverToken1);
			var challenge = sasl.Challenge (Convert.ToBase64String (token));
			var decoded = Convert.FromBase64String (challenge);
			var result = Encoding.ASCII.GetString (decoded);

			Assert.AreEqual (expected1, result, "DIGEST-MD5 challenge response does not match the expected string.");
			Assert.IsFalse (sasl.IsAuthenticated, "DIGEST-MD5 should not be authenticated yet.");

			token = Encoding.ASCII.GetBytes (serverToken2);
			challenge = sasl.Challenge (Convert.ToBase64String (token));
			decoded = Convert.FromBase64String (challenge);
			result = Encoding.ASCII.GetString (decoded);

			Assert.AreEqual (string.Empty, result, "Second DIGEST-MD5 challenge should be an empty string.");
			Assert.IsTrue (sasl.IsAuthenticated, "DIGEST-MD5 should be authenticated now.");
		}

		[Test]
		public void TestLoginAuth ()
		{
			const string expected1 = "dXNlcm5hbWU=";
			const string expected2 = "cGFzc3dvcmQ=";
			var credentials = new NetworkCredential ("username", "password");
			var uri = new Uri ("imap://imap.gmail.com");
			var sasl = new SaslMechanismLogin (uri, credentials);
			string challenge;

			challenge = sasl.Challenge (string.Empty);

			Assert.AreEqual (expected1, challenge, "LOGIN initial challenge response does not match the expected string.");
			Assert.IsFalse (sasl.IsAuthenticated, "LOGIN should not be authenticated.");

			challenge = sasl.Challenge (string.Empty);

			Assert.AreEqual (expected2, challenge, "LOGIN final challenge response does not match the expected string.");
			Assert.IsTrue (sasl.IsAuthenticated, "LOGIN should be authenticated.");
		}

		[Test]
		public void TestPlainAuth ()
		{
			const string expected = "AHVzZXJuYW1lAHBhc3N3b3Jk";
			var credentials = new NetworkCredential ("username", "password");
			var uri = new Uri ("imap://imap.gmail.com");
			var sasl = new SaslMechanismPlain (uri, credentials);

			var challenge = sasl.Challenge (string.Empty);

			Assert.AreEqual (expected, challenge, "PLAIN challenge response does not match the expected string.");
			Assert.IsTrue (sasl.IsAuthenticated, "PLAIN should be authenticated.");
		}

		static string HexEncode (byte[] message)
		{
			var builder = new StringBuilder ();

			for (int i = 0; i < message.Length; i++)
				builder.Append (message[i].ToString ("x2"));

			return builder.ToString ();
		}

		static readonly byte[] NtlmType1EncodedMessage = {
			0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00, 0x01, 0x00, 0x00, 0x00,
			0x07, 0x32, 0x00, 0x00, 0x06, 0x00, 0x06, 0x00, 0x33, 0x00, 0x00, 0x00,
			0x0b, 0x00, 0x0b, 0x00, 0x28, 0x00, 0x00, 0x00, 0x05, 0x00, 0x93, 0x08,
			0x00, 0x00, 0x00, 0x0f, 0x57, 0x4f, 0x52, 0x4b, 0x53, 0x54, 0x41, 0x54,
			0x49, 0x4f, 0x4e, 0x44, 0x4f, 0x4d, 0x41, 0x49, 0x4e
		};

		[Test]
		public void TestNtlmType1MessageEncode ()
		{
			var type1 = new Type1Message ("Workstation", "Domain") { OSVersion = new Version (5, 0, 2195) };
			var encoded = type1.Encode ();
			string actual, expected;

			expected = HexEncode (NtlmType1EncodedMessage);
			actual = HexEncode (encoded);

			Assert.AreEqual (expected, actual, "The encoded Type1Message did not match the expected result.");
		}

		[Test]
		public void TestNtlmType1MessageDecode ()
		{
			var flags = NtlmFlags.NegotiateUnicode | NtlmFlags.NegotiateDomainSupplied | NtlmFlags.NegotiateWorkstationSupplied |
				NtlmFlags.NegotiateNtlm | NtlmFlags.NegotiateOem | NtlmFlags.RequestTarget;
			var type1 = new Type1Message (NtlmType1EncodedMessage, 0, NtlmType1EncodedMessage.Length);

			Assert.AreEqual (flags, type1.Flags, "The expected flags do not match.");
			Assert.AreEqual ("WORKSTATION", type1.Host, "The expected workstation name does not match.");
			Assert.AreEqual ("DOMAIN", type1.Domain, "The expected domain does not match.");
			Assert.AreEqual (new Version (5, 0, 2195), type1.OSVersion, "The expected OS Version does not match.");
		}

		static readonly byte[] NtlmType2EncodedMessage = {
			0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00, 0x02, 0x00, 0x00, 0x00,
			0x0c, 0x00, 0x0c, 0x00, 0x30, 0x00, 0x00, 0x00, 0x01, 0x02, 0x81, 0x00,
			0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x62, 0x00, 0x62, 0x00, 0x3c, 0x00, 0x00, 0x00,
			0x44, 0x00, 0x4f, 0x00, 0x4d, 0x00, 0x41, 0x00, 0x49, 0x00, 0x4e, 0x00,
			0x02, 0x00, 0x0c, 0x00, 0x44, 0x00, 0x4f, 0x00, 0x4d, 0x00, 0x41, 0x00,
			0x49, 0x00, 0x4e, 0x00, 0x01, 0x00, 0x0c, 0x00, 0x53, 0x00, 0x45, 0x00,
			0x52, 0x00, 0x56, 0x00, 0x45, 0x00, 0x52, 0x00, 0x04, 0x00, 0x14, 0x00,
			0x64, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x61, 0x00, 0x69, 0x00, 0x6e, 0x00,
			0x2e, 0x00, 0x63, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x03, 0x00, 0x22, 0x00,
			0x73, 0x00, 0x65, 0x00, 0x72, 0x00, 0x76, 0x00, 0x65, 0x00, 0x72, 0x00,
			0x2e, 0x00, 0x64, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x61, 0x00, 0x69, 0x00,
			0x6e, 0x00, 0x2e, 0x00, 0x63, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x00, 0x00,
			0x00, 0x00
		};

		[Test]
		public void TestNtlmType2MessageDecode ()
		{
			const string expectedTargetInfo = "02000c0044004f004d00410049004e0001000c005300450052005600450052000400140064006f006d00610069006e002e0063006f006d00030022007300650072007600650072002e0064006f006d00610069006e002e0063006f006d0000000000";
			var flags = NtlmFlags.NegotiateUnicode | NtlmFlags.NegotiateNtlm | NtlmFlags.TargetTypeDomain | NtlmFlags.NegotiateTargetInfo;
			var type2 = new Type2Message (NtlmType2EncodedMessage, 0, NtlmType2EncodedMessage.Length);

			Assert.AreEqual (flags, type2.Flags, "The expected flags do not match.");
			Assert.AreEqual ("DOMAIN", type2.TargetName, "The expected TargetName does not match.");

			var nonce = HexEncode (type2.Nonce);
			Assert.AreEqual ("0123456789abcdef", nonce, "The expected nonce does not match.");

			var targetInfo = HexEncode (type2.TargetInfo);
			Assert.AreEqual (expectedTargetInfo, targetInfo, "The expected TargetInfo does not match.");
		}

		[Test]
		public void TestNtlmAuthNoDomain ()
		{
			const NtlmFlags initialFlags = NtlmFlags.NegotiateUnicode | NtlmFlags.NegotiateOem | NtlmFlags.NegotiateNtlm |
				NtlmFlags.NegotiateNtlm2Key | NtlmFlags.RequestTarget;
			var credentials = new NetworkCredential ("username", "password");
			var uri = new Uri ("imap://imap.gmail.com");
			var sasl = new SaslMechanismNtlm (uri, credentials);
			string challenge;
			byte[] decoded;

			challenge = sasl.Challenge (string.Empty);
			decoded = Convert.FromBase64String (challenge);

			var type1 = new Type1Message (decoded, 0, decoded.Length);

			Assert.AreEqual (initialFlags, type1.Flags, "Expected initial NTLM client challenge flags do not match.");
			Assert.AreEqual (string.Empty, type1.Domain, "Expected initial NTLM client challenge domain does not match.");
			Assert.AreEqual (string.Empty, type1.Host, "Expected initial NTLM client challenge host does not match.");
			Assert.IsFalse (sasl.IsAuthenticated, "NTLM should not be authenticated.");
		}

		[Test]
		public void TestNtlmAuthWithDomain ()
		{
			const NtlmFlags initialFlags = NtlmFlags.NegotiateUnicode | NtlmFlags.NegotiateOem | NtlmFlags.NegotiateNtlm |
				NtlmFlags.NegotiateNtlm2Key | NtlmFlags.RequestTarget | NtlmFlags.NegotiateDomainSupplied;
			var credentials = new NetworkCredential ("domain\\username", "password");
			var uri = new Uri ("imap://imap.gmail.com");
			var sasl = new SaslMechanismNtlm (uri, credentials);
			string challenge;
			byte[] decoded;

			challenge = sasl.Challenge (string.Empty);
			decoded = Convert.FromBase64String (challenge);

			var type1 = new Type1Message (decoded, 0, decoded.Length);

			Assert.AreEqual (initialFlags, type1.Flags, "Expected initial NTLM client challenge flags do not match.");
			Assert.AreEqual ("DOMAIN", type1.Domain, "Expected initial NTLM client challenge domain does not match.");
			Assert.AreEqual (string.Empty, type1.Host, "Expected initial NTLM client challenge host does not match.");
			Assert.IsFalse (sasl.IsAuthenticated, "NTLM should not be authenticated.");
		}
	}
}

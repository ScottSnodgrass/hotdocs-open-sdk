﻿/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using HotDocs.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HotDocs.SdkTest
{
	[TestClass]
	public class AnswerSetTest
	{
		[TestMethod]
		public void ReadXml()
		{
			AnswerCollection anss = new AnswerCollection();
			// this test checks some answer XML generated by browser interviews. It was problematic originally because of its empty <RptValue> elements
			// (expressed as paired open/close elements with nothing between them, rather than single elements with open/close combined in the same element)
			anss.ReadXml(@"<?xml version=""1.0"" standalone=""yes""?>
<AnswerSet title="""" version=""1.1"" useMangledNames=""false"">
	<Answer name=""Editor Full Name"">
		<TextValue unans=""true"" />
	</Answer>
	<Answer name=""Author Full Name"">
		<RptValue>
			<RptValue>
				<TextValue>A</TextValue>
				<TextValue unans=""true"" />
			</RptValue>
			<RptValue></RptValue>
		</RptValue>
	</Answer>
	<Answer name=""Book Title"">
		<RptValue>
			<RptValue>
				<RptValue>
					<TextValue>A</TextValue>
					<TextValue unans=""true"" />
				</RptValue>
				<RptValue></RptValue>
			</RptValue>
			<RptValue></RptValue>
		</RptValue>
	</Answer>
	<Answer name=""Date Completed"">
		<DateValue unans=""true"" />
	</Answer>
</AnswerSet>
");
			Assert.IsTrue(anss.AnswerCount == 4);
			Answer ans;
			Assert.IsTrue(anss.TryGetAnswer("Editor Full Name", out ans));
			Assert.IsFalse(ans.IsRepeated);
			Assert.IsTrue(ans.Save);
			Assert.IsTrue(ans.UserExtendible);
			Assert.IsTrue(ans.Type == Sdk.ValueType.Text);
			Assert.IsFalse(ans.GetAnswered());
			Assert.IsFalse(ans.GetValue<TextValue>().IsAnswered);
			Assert.IsTrue(ans.GetValue<TextValue>().Type == Sdk.ValueType.Text);
			Assert.IsTrue(ans.GetValue<TextValue>().UserModifiable);

			Assert.IsFalse(anss.TryGetAnswer("author full name", out ans));

			Assert.IsTrue(anss.TryGetAnswer("Author Full Name", out ans));
			Assert.IsTrue(ans.IsRepeated);
			Assert.IsTrue(ans.GetChildCount() == 1);
			Assert.IsTrue(ans.GetChildCount(0) == 1);
			Assert.IsTrue(ans.GetChildCount(1) == 0);
			Assert.IsTrue(ans.GetValue<TextValue>(0, 0).Value == "A");
			Assert.IsFalse(ans.GetValue<TextValue>(0, 1).IsAnswered);
			Assert.IsFalse(ans.GetValue<TextValue>(1).IsAnswered);
			Assert.IsFalse(ans.GetValue<TextValue>(1, 0).IsAnswered);
			// unusual HotDocs indexing rules
			Assert.IsTrue(ans.GetValue<TextValue>().IsAnswered);
			Assert.IsTrue(ans.GetValue<TextValue>().Value == "A");
			Assert.IsTrue(ans.GetValue<TextValue>(0).IsAnswered);
			Assert.IsTrue(ans.GetValue<TextValue>(0).Value == "A");
			Assert.IsTrue(ans.GetValue<TextValue>(0, 0, 0).IsAnswered);
			Assert.IsTrue(ans.GetValue<TextValue>(0, 0, 0).Value == "A");


			Assert.IsFalse(anss.TryGetAnswer("does not exist", out ans));
		}
	}
}
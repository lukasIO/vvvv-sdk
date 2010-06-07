﻿// CSharp Editor Example with Code Completion
// Copyright (c) 2006, Daniel Grunwald
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
// 
// - Redistributions of source code must retain the above copyright notice, this list
//   of conditions and the following disclaimer.
// 
// - Redistributions in binary form must reproduce the above copyright notice, this list
//   of conditions and the following disclaimer in the documentation and/or other materials
//   provided with the distribution.
// 
// - Neither the name of the ICSharpCode team nor the names of its contributors may be used to
//   endorse or promote products derived from this software without specific prior written
//   permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS &AS IS& AND ANY EXPRESS
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

using Dom = ICSharpCode.SharpDevelop.Dom;
using NRefactoryResolver = ICSharpCode.SharpDevelop.Dom.NRefactoryResolver.NRefactoryResolver;

using VVVV.Nodes;

namespace CSharpEditor
{
	class CodeCompletionProvider : ICompletionDataProvider
	{
		CodeEditor FCodeEditor;
		
		public CodeCompletionProvider(CodeEditor codeEditor)
		{
			this.FCodeEditor = codeEditor;
		}
		
		public ImageList ImageList {
			get {
				return FCodeEditor.ImageList;
			}
		}
		
		private string FPreSelection = null;
		public string PreSelection 
		{ 
			get
			{
				return FPreSelection;
			}
			protected set
			{
				FPreSelection = value;
			}
		}
		
		private int FDefaultIndex = -1;
		public int DefaultIndex 
		{
			get 
			{
				return FDefaultIndex;
			}
			protected set 
			{
				FDefaultIndex = value;
			}
		}
		
		public CompletionDataProviderKeyResult ProcessKey(char key)
		{
			if (char.IsLetterOrDigit(key) || key == '_') 
			{
				return CompletionDataProviderKeyResult.NormalKey;
			} 
			else 
			{
				// key triggers insertion of selected items
				return CompletionDataProviderKeyResult.InsertionKey;
			}
		}
		
		/// <summary>
		/// Called when entry should be inserted. Forward to the insertion action of the completion data.
		/// </summary>
		public bool InsertAction(ICompletionData data, TextArea textArea, int insertionOffset, char key)
		{
			textArea.Caret.Position = textArea.Document.OffsetToPosition(insertionOffset);
			return data.InsertAction(textArea, key);
		}
		
		public ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped)
		{
			// We can return code-completion items like this:
			
			//return new ICompletionData[] {
			//	new DefaultCompletionData("Text", "Description", 1)
			//};
			
			var resolver = new NRefactoryResolver(FCodeEditor.FProjectContent.Language);
			
			var resultList = new List<ICompletionData>();
			var expressionResult = FindExpression(textArea);
			
			Debug.WriteLine(String.Format("Generating completion data for expression result {0}", expressionResult));

			ArrayList completionData = null;
			if (charTyped == '.')
			{
				FPreSelection = null;
				var rr = resolver.Resolve(expressionResult,
				                          FCodeEditor.FParseInfo,
				                          textArea.MotherTextEditorControl.Text);
				
				if (rr != null)
					completionData = rr.GetCompletionData(FCodeEditor.FProjectContent);
			}
			else
			{
				FPreSelection = "";
				completionData = resolver.CtrlSpace(textArea.Caret.Line + 1, 
				                                    textArea.Caret.Column + 1, 
				                                    FCodeEditor.FParseInfo, 
				                                    textArea.Document.TextContent,
				                                    expressionResult.Context);
			}
			
			if (completionData != null)
				AddCompletionData(ref resultList, completionData, expressionResult.Context);
			
			return resultList.ToArray();
		}
		
		/// <summary>
		/// Find the expression the cursor is at.
		/// Also determines the context (using statement, "new"-expression etc.) the
		/// cursor is at.
		/// </summary>
		Dom.ExpressionResult FindExpression(TextArea textArea)
		{
			var document = textArea.Document;
			var finder = new Dom.CSharp.CSharpExpressionFinder(FCodeEditor.FParseInfo);

			var expression = finder.FindExpression(document.GetText(0, textArea.Caret.Offset), textArea.Caret.Offset);
			if (expression.Region.IsEmpty) {
				expression.Region = new Dom.DomRegion(textArea.Caret.Line + 1, textArea.Caret.Column + 1);
			}
			return expression;
		}

		void AddCompletionData(ref List<ICompletionData> resultList, ArrayList completionData, Dom.ExpressionContext context)
		{
			// used to store the method names for grouping overloads
			Dictionary<string, CodeCompletionData> nameDictionary = new Dictionary<string, CodeCompletionData>();

			// Add the completion data as returned by SharpDevelop.Dom to the
			// list for the text editor
			foreach (object obj in completionData) {
				if (!context.ShowEntry(obj))
					continue;
				
				if (obj is string) {
					// namespace names are returned as string
					resultList.Add(new DefaultCompletionData((string)obj, "namespace " + obj, 5));
				} else if (obj is Dom.IClass) {
					Dom.IClass c = (Dom.IClass)obj;
					resultList.Add(new CodeCompletionData(c));
				} else if (obj is Dom.IMember) {
					Dom.IMember m = (Dom.IMember)obj;
					if (m is Dom.IMethod && ((m as Dom.IMethod).IsConstructor)) {
						// Skip constructors
						continue;
					}
					// Group results by name and add "(x Overloads)" to the
					// description if there are multiple results with the same name.
					
					CodeCompletionData data;
					if (nameDictionary.TryGetValue(m.Name, out data)) {
						data.AddOverload();
					} else {
						nameDictionary[m.Name] = data = new CodeCompletionData(m);
						resultList.Add(data);
					}
				} else {
					// Current ICSharpCode.SharpDevelop.Dom should never return anything else
					throw new NotSupportedException();
				}
			}
		}
	}
}
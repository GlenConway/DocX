﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Novacode;
using NUnit.Framework;
using DocXFormatting = Novacode.Formatting;
using WindowsColor = System.Drawing.Color;

namespace UnitTests
{
    /// <summary>
    /// Summary description for DocXUnitTest
    /// </summary>
    [TestFixture]
    public class DocXUnitTests
    {
        private readonly string _directoryDocuments;
        private readonly string _directoryWithFiles;
        private static readonly Border BlankBorder = new Border(BorderStyle.Tcbs_none, 0, 0, WindowsColor.White);

        const string package_part_document = "/word/document.xml";

        public DocXUnitTests()
        {
            _directoryDocuments = Path.Combine(Path.GetTempPath(), "DocXTests", "documents");
            Setup(_directoryDocuments); // prepare temp documents directory 

            _directoryWithFiles = TestHelper.DirectoryWithFiles;
        }
        private static void Setup(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        [Test]
        public void LargeTable()
        {
            using (var output = File.Open(Path.Combine(_directoryWithFiles, "LargeTable.docx"), FileMode.Create))
            {
                using (var doc = DocX.Create(output))
                {
                    var tbl = doc.InsertTable(1, 18);

                    var wholeWidth = doc.PageWidth - doc.MarginLeft - doc.MarginRight;
                    var colWidth = wholeWidth / tbl.ColumnCount;
                    var colWidths = new int[tbl.ColumnCount];
                    tbl.AutoFit = AutoFit.Contents;
                    var r = tbl.Rows[0];
                    var cx = 0;
                    foreach (var cell in r.Cells)
                    {
                        cell.Paragraphs.First().Append("Col " + cx);
                        cell.MarginBottom = 0;
                        cell.MarginLeft = 0;
                        cell.MarginRight = 0;
                        cell.MarginTop = 0;

                        cx++;
                    }
                    tbl.SetBorder(TableBorderType.Bottom, BlankBorder);
                    tbl.SetBorder(TableBorderType.Left, BlankBorder);
                    tbl.SetBorder(TableBorderType.Right, BlankBorder);
                    tbl.SetBorder(TableBorderType.Top, BlankBorder);
                    tbl.SetBorder(TableBorderType.InsideV, BlankBorder);
                    tbl.SetBorder(TableBorderType.InsideH, BlankBorder);

                    doc.Save();
                }
            }
        }

		[Test]
		public void Test_DocX_SaveAs()
		{
			string temporaryFilePath = Path.Combine( _directoryDocuments, "temp.docx" );

			// Load the document 'Paragraphs.docx'
			using( DocX document = DocX.Load( Path.Combine( _directoryWithFiles, "Paragraphs.docx" ) ) )
			{
				document.InsertParagraph( "text" );

				// Test the saving of the document.
				document.SaveAs( temporaryFilePath );

				// Check file size
				FileInfo f = new FileInfo( temporaryFilePath );
				Assert.IsTrue( f.Length == 9658 );
			}

			// Delete the tempory file.
			File.Delete( temporaryFilePath );
		}

		[Test]
        public void TableWithSpecifiedWidths()
        {
            using (var output = File.Open(Path.Combine(_directoryWithFiles, "TableSpecifiedWidths.docx"), FileMode.Create))
            {
                using (var doc = DocX.Create(output))
                {
                    var widths = new float[] { 200f, 100f, 300f };
                    var tbl = doc.InsertTable(1, widths.Length);
                    tbl.SetWidths(widths);
                    var wholeWidth = doc.PageWidth - doc.MarginLeft - doc.MarginRight;
                    tbl.AutoFit = AutoFit.Contents;
                    var r = tbl.Rows[0];
                    var cx = 0;
                    foreach (var cell in r.Cells)
                    {
                        cell.Paragraphs.First().Append("Col " + cx);
                        cell.MarginBottom = 0;
                        cell.MarginLeft = 0;
                        cell.MarginRight = 0;
                        cell.MarginTop = 0;

                        cx++;
                    }
                    //add new rows 
                    for (var x = 0; x < 5; x++)
                    {
                        r = tbl.InsertRow();
                        cx = 0;
                        foreach (var cell in r.Cells)
                        {
                            cell.Paragraphs.First().Append("Col " + cx);
                            cell.MarginBottom = 0;
                            cell.MarginLeft = 0;
                            cell.MarginRight = 0;
                            cell.MarginTop = 0;

                            cx++;
                        }
                    }
                    tbl.SetBorder(TableBorderType.Bottom, BlankBorder);
                    tbl.SetBorder(TableBorderType.Left, BlankBorder);
                    tbl.SetBorder(TableBorderType.Right, BlankBorder);
                    tbl.SetBorder(TableBorderType.Top, BlankBorder);
                    tbl.SetBorder(TableBorderType.InsideV, BlankBorder);
                    tbl.SetBorder(TableBorderType.InsideH, BlankBorder);

                    doc.Save();
                }
            }
        }

		[Test]
		public void Test_InvalidCharacter()
		{
			using( var output = File.Open( Path.Combine( _directoryWithFiles, "InvalidCharacters.docx" ), FileMode.Create ) )
			{
				using( var doc = DocX.Create( output ) )
				{
					doc.InsertParagraph( "\b" );
					Exception ex = null;
					try
					{
						doc.Save();
					}
					catch( Exception e )
					{
						ex = e;
					}
					Assert.IsTrue( ex == null );
				}
			}
		}

		/// <summary>
		/// TextRemove should not remove empty paragraphs in case the paragraph is alone in the cell.
		/// In the rest cases empty paragraph may be removed.
		/// </summary>
		[Test]
		public void Test_Table_Paragraph_RemoveText()
		{
			using( var input = File.Open( Path.Combine( _directoryWithFiles, "TableSpecifiedHeights.docx" ), FileMode.Open ) )
			{
				using( var doc = DocX.Load( input ) )
				{
					// Make sure content of the file is ok for test
					Assert.IsTrue( doc.Tables.Count == 1 );
					Assert.IsTrue( doc.Tables[ 0 ].RowCount == 3 );

					string text = "paragraph";

					// == Paragraph in the cell is not alone ==
					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].InsertParagraph( text );
					Assert.IsTrue( doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs.Count == 2 );

					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].ReplaceText( text, "", removeEmptyParagraph: true );
					Assert.IsTrue( doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs.Count == 1 );

					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].InsertParagraph( text );
					Assert.IsTrue( doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs.Count == 2 );

					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].ReplaceText( text, "", removeEmptyParagraph: false );
					Assert.IsTrue( doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs.Count == 2 );

					// == Paragraph in the cell is alone ==
					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].InsertParagraph( text );
					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs[ 0 ].Remove( false );
					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs[ 0 ].Remove( false );
					Assert.IsTrue( doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs.Count == 1 );

					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].ReplaceText( text, "", removeEmptyParagraph: true );
					Assert.IsTrue( doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs.Count == 1 );

					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].InsertParagraph( text );
					Assert.IsTrue( doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs.Count == 2 );

					doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].ReplaceText( text, "", removeEmptyParagraph: false );
					Assert.IsTrue( doc.Tables[ 0 ].Rows[ 0 ].Cells[ 0 ].Paragraphs.Count == 2 );
				}
			}
		}

		[Test]
		public void Test_Table_MinHeight()
		{
			using( var input = File.Open( Path.Combine( _directoryWithFiles, "TableSpecifiedHeights.docx" ), FileMode.Open ) )
			{
				using( var doc = DocX.Load( input ) )
				{
					// Make sure content of the file is ok for test
					Assert.IsTrue( doc.Tables.Count == 1 );
					Assert.IsTrue( doc.Tables[ 0 ].RowCount == 3 );

					// Check heights load is ok
					Assert.IsTrue( double.IsNaN( doc.Tables[ 0 ].Rows[ 0 ].Height ) );
					Assert.IsTrue( double.IsNaN( doc.Tables[ 0 ].Rows[ 0 ].MinHeight ) );
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 1 ].Height - 37.8f ) < 0.0001f );
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 1 ].MinHeight - 37.8f ) < 0.0001f );
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 2 ].Height - 37.8f ) < 0.0001f );
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 2 ].MinHeight - 37.8f ) < 0.0001f );

					// Set MinHeight
					doc.Tables[ 0 ].Rows[ 0 ].MinHeight = 37.8f;
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 0 ].Height - 37.8f ) < 0.0001f );
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 0 ].MinHeight - 37.8f ) < 0.0001f );
				}
			}
		}

		[Test]
		public void Test_Table_InsertRow_Keeps_Formatting()
		{
			using( var input = File.Open( Path.Combine( _directoryWithFiles, "TableSpecifiedHeights.docx" ), FileMode.Open ) )
			{
				using( var doc = DocX.Load( input ) )
				{
					// Make sure content of the file is ok for test
					Assert.IsTrue( doc.Tables.Count == 1 );
					Assert.IsTrue( doc.Tables[ 0 ].RowCount == 3 );

					// Check heights load is ok
					Assert.IsTrue( double.IsNaN( doc.Tables[ 0 ].Rows[ 0 ].Height ) );
					Assert.IsTrue( double.IsNaN( doc.Tables[ 0 ].Rows[ 0 ].MinHeight ) );
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 1 ].Height - 37.8f ) < 0.0001f );
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 1 ].MinHeight - 37.8f ) < 0.0001f );
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 2 ].Height - 37.8f ) < 0.0001f );
					Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ 2 ].MinHeight - 37.8f ) < 0.0001f );

					// Clone all rows and check heights
					int n = doc.Tables[ 0 ].RowCount;
					for( int index = 0; index < n; index++ )
					{
						doc.Tables[ 0 ].InsertRow( doc.Tables[ 0 ].Rows[ index ], true );
					}
					Assert.IsTrue( doc.Tables[ 0 ].RowCount == 2 * n );
					for( int index = 0; index < n; index++ )
					{
						// Compare height of original row and cloned
						Assert.IsTrue( double.IsNaN( doc.Tables[ 0 ].Rows[ n + index ].Height ) == double.IsNaN( doc.Tables[ 0 ].Rows[ index ].Height ) );
						if( !double.IsNaN( doc.Tables[ 0 ].Rows[ n + index ].Height ) && !double.IsNaN( doc.Tables[ 0 ].Rows[ index ].Height ) )
							Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ n + index ].Height - doc.Tables[ 0 ].Rows[ index ].Height ) < 0.0001f );
						Assert.IsTrue( double.IsNaN( doc.Tables[ 0 ].Rows[ n + index ].MinHeight ) == double.IsNaN( doc.Tables[ 0 ].Rows[ index ].MinHeight ) );
						if( !double.IsNaN( doc.Tables[ 0 ].Rows[ n + index ].MinHeight ) && !double.IsNaN( doc.Tables[ 0 ].Rows[ index ].MinHeight ) )
							Assert.IsTrue( Math.Abs( doc.Tables[ 0 ].Rows[ n + index ].MinHeight - doc.Tables[ 0 ].Rows[ index ].MinHeight ) < 0.0001f );
					}
					// Remove original rows
					for( int index = 0; index < n; index++ )
					{
						doc.Tables[ 0 ].Rows[ 0 ].Remove();
					}

					// At this point we shuold have document visually equal to original

					doc.SaveAs( Path.Combine( _directoryWithFiles, "TableSpecifiedHeights_out.docx" ) );
				}
			}
		}

		[Test]
		public void Test_Clone_Table_Twice()
		{
			using( var input = File.Open( Path.Combine( _directoryWithFiles, "TableSpecifiedHeights.docx" ), FileMode.Open ) )
			{
				using( var doc = DocX.Load( input ) )
				{
					// Make sure content of the file is ok for test
					Assert.IsTrue( doc.Tables.Count == 1 );

					Table tab1 = doc.Tables[ 0 ];
					doc.InsertParagraph( "" );
					Table tab2 = doc.InsertTable( tab1 );
					Assert.IsTrue( doc.Tables.Count == 2 );
					doc.InsertParagraph( "" );
					Table tab3 = doc.InsertTable( tab2 );
					Assert.IsTrue( doc.Tables.Count == 3 );

					doc.SaveAs( Path.Combine( _directoryWithFiles, "TwoClonedTables.docx" ) );
				}
			}
		}

		public string ReplaceFunc(string findStr)
        {
            var testPatterns = new Dictionary<string, string>
            {
                {"COURT NAME","Fred Frump"},
                {"Case Number","cr-md-2011-1234567"}
            };

            if (testPatterns.ContainsKey(findStr))
            {
                return testPatterns[findStr];
            }
            return findStr;
        }

        [Test]
        public void RegexTest()
        {
            var findPattern = "<(.*?)>";
            var sample = "<Match This> text";
            var matchCollection = Regex.Matches(sample, findPattern, RegexOptions.IgnoreCase);
        }

        [Test]
        public void Test_Pattern_Replacement()
        {
            var testPatterns = new Dictionary<string, string>
            {
                {"COURT NAME","Fred Frump"},
                {"Case Number","cr-md-2011-1234567"}
            };

            using (DocX replaceDoc = DocX.Load(Path.Combine(_directoryWithFiles, "ReplaceTests.docx")))
            {
                foreach (var t in replaceDoc.Tables)
                {   // each table has 1 row and 3 columns
                    Assert.IsTrue(t.Rows[0].Cells.Count == 3);
                    Assert.IsTrue(t.ColumnCount == 3);
                    Assert.IsTrue(t.Rows.Count == 1);
                    Assert.IsTrue(t.RowCount == 1);
                }

                // Make sure the origional strings are in the document.
                Assert.IsTrue(replaceDoc.FindAll("<COURT NAME>").Count == 2);
                Assert.IsTrue(replaceDoc.FindAll("<Case Number>").Count == 2);

                // There are only two patterns, even though each pattern is used more than once
                Assert.IsTrue(replaceDoc.FindUniqueByPattern(@"<[\w \=]{4,}>", RegexOptions.IgnoreCase).Count == 2);

                // Make sure the new strings are not in the document.
                Assert.IsTrue(replaceDoc.FindAll("Fred Frump").Count == 0);
                Assert.IsTrue(replaceDoc.FindAll("cr-md-2011-1234567").Count == 0);

                // Do the replacing
                foreach (var p in testPatterns)
                {
                    replaceDoc.ReplaceText("<(.*?)>", ReplaceFunc, false, RegexOptions.IgnoreCase);
                    //replaceDoc.ReplaceText("<" + p.Key + ">", p.Value, false, RegexOptions.IgnoreCase);
                }

                // Make sure the origional string are no longer in the document.
                Assert.IsTrue(replaceDoc.FindAll("<COURT NAME>").Count == 0);
                Assert.IsTrue(replaceDoc.FindAll("<Case Number>").Count == 0);

                // Make sure the new strings are now in the document.
                Assert.IsTrue(replaceDoc.FindAll("FRED FRUMP").Count == 2);
                Assert.IsTrue(replaceDoc.FindAll("cr-md-2011-1234567").Count == 2);

                // Make sure the replacement worked.
                Assert.IsTrue(replaceDoc.Text == "\t\t\t\t\t\t\t\t\t\t\t\t\t\tThese two tables should look identical:\t\t\t\t\t\t\t\t\t\t\t\t\t\t\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tSTATE OF IOWA,\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tPlaintiff,\t\t\t\t\t\t\t\t\t\t\t\t\t\tvs.\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tFRED FRUMP,\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tDefendant.\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tCase No.: cr-md-2011-1234567\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tORDER SETTING ASIDE DEFAULT JUDGMENT\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tSTATE OF IOWA,\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tPlaintiff,\t\t\t\t\t\t\t\t\t\t\t\t\t\tvs.\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tFRED FRUMP,\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tDefendant.\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tCase No.: cr-md-2011-1234567\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tORDER SETTING ASIDE DEFAULT JUDGMENT\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            }

        }

        [Test]
        public void Test_CustomProperty_Add()
        {
            // Load a document.
            using (DocX document = DocX.Create("CustomProperty_Add.docx"))
            {
                Assert.IsTrue(document.CustomProperties.Count == 0);

                document.AddCustomProperty(new CustomProperty("fname", "cathal"));

                Assert.IsTrue(document.CustomProperties.Count == 1);
                Assert.IsTrue(document.CustomProperties.ContainsKey("fname"));
                Assert.IsTrue((string)document.CustomProperties["fname"].Value == "cathal");

                document.AddCustomProperty(new CustomProperty("age", 24));

                Assert.IsTrue(document.CustomProperties.Count == 2);
                Assert.IsTrue(document.CustomProperties.ContainsKey("age"));
                Assert.IsTrue((int)document.CustomProperties["age"].Value == 24);

                document.AddCustomProperty(new CustomProperty("male", true));

                Assert.IsTrue(document.CustomProperties.Count == 3);
                Assert.IsTrue(document.CustomProperties.ContainsKey("male"));
                Assert.IsTrue((bool)document.CustomProperties["male"].Value);

                document.AddCustomProperty(new CustomProperty("newyear2012", new DateTime(2012, 1, 1)));

                Assert.IsTrue(document.CustomProperties.Count == 4);
                Assert.IsTrue(document.CustomProperties.ContainsKey("newyear2012"));
                Assert.IsTrue((DateTime)document.CustomProperties["newyear2012"].Value == new DateTime(2012, 1, 1));

                document.AddCustomProperty(new CustomProperty("fav_num", 3.141592));

                Assert.IsTrue(document.CustomProperties.Count == 5);
                Assert.IsTrue(document.CustomProperties.ContainsKey("fav_num"));
                Assert.IsTrue((double)document.CustomProperties["fav_num"].Value == 3.141592);
            }
        }

        [Test]
        public void Test_EverybodyHasAHome_Loaded()
        {
            // Load a document.
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "EverybodyHasAHome.docx")))
            {
                // Main document tests.
                string document_xml_file = document.mainPart.Uri.OriginalString;
                Assert.IsTrue(document.Paragraphs[0].mainPart.Uri.OriginalString.Equals(document_xml_file));
                Assert.IsTrue(document.Tables[0].mainPart.Uri.OriginalString.Equals(document_xml_file));
                Assert.IsTrue(document.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(document_xml_file));
                Assert.IsTrue(document.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(document_xml_file));
                Assert.IsTrue(document.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(document_xml_file));

                // header first
                Header header_first = document.Headers.first;
                string header_first_xml_file = header_first.mainPart.Uri.OriginalString;

                Assert.IsTrue(header_first.Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));
                Assert.IsTrue(header_first.Tables[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));
                Assert.IsTrue(header_first.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));
                Assert.IsTrue(header_first.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));
                Assert.IsTrue(header_first.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));

                // header odd
                Header header_odd = document.Headers.odd;
                string header_odd_xml_file = header_odd.mainPart.Uri.OriginalString;

                Assert.IsTrue(header_odd.mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Tables[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));

                // header even
                Header header_even = document.Headers.even;
                string header_even_xml_file = header_even.mainPart.Uri.OriginalString;

                Assert.IsTrue(header_even.mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Tables[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));

                // footer first
                Footer footer_first = document.Footers.first;
                string footer_first_xml_file = footer_first.mainPart.Uri.OriginalString;

                Assert.IsTrue(footer_first.mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Tables[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));

                // footer odd
                Footer footer_odd = document.Footers.odd;
                string footer_odd_xml_file = footer_odd.mainPart.Uri.OriginalString;

                Assert.IsTrue(footer_odd.mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Tables[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));

                // footer even
                Footer footer_even = document.Footers.even;
                string footer_even_xml_file = footer_even.mainPart.Uri.OriginalString;

                Assert.IsTrue(footer_even.mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Tables[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
            }
        }

        [Test]
        public void Test_Insert_Picture_ParagraphBeforeSelf()
        {
            // Create test document.
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Test.docx")))
            {
                // Add an Image to this document.
                Image img = document.AddImage(Path.Combine(_directoryWithFiles, "purple.png"));

                // Create a Picture from this Image.
                Picture pic = img.CreatePicture();
                Assert.IsNotNull(pic);

                // Main document.
                Paragraph p0 = document.InsertParagraph("Hello");
                Paragraph p1 = p0.InsertParagraphBeforeSelf("again");
                p1.InsertPicture(pic, 3);

                // Save this document.
                document.Save();
            }
        }

        [Test]
        public void Test_Insert_Picture_ParagraphAfterSelf()
        {
            // Create test document.
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Test.docx")))
            {
                // Add an Image to this document.
                Image img = document.AddImage(Path.Combine(_directoryWithFiles, "purple.png"));

                // Create a Picture from this Image.
                Picture pic = img.CreatePicture();
                Assert.IsNotNull(pic);

                // Main document.
                Paragraph p0 = document.InsertParagraph("Hello");
                Paragraph p1 = p0.InsertParagraphAfterSelf("again");
                p1.InsertPicture(pic, 3);

                // Save this document.
                document.Save();
            }
        }

        [Test]
        public void Test_EverybodyHasAHome_Created()
        {
            // Create a new document.
            using (DocX document = DocX.Create("Test.docx"))
            {
                // Create a Table.
                Table t = document.AddTable(3, 3);
                t.Design = TableDesign.TableGrid;

                // Insert a Paragraph and a Table into the main document.
                document.InsertParagraph();
                document.InsertTable(t);

                // Insert a Paragraph and a Table into every Header.
                document.AddHeaders();
                document.Headers.odd.InsertParagraph();
                document.Headers.odd.InsertTable(t);
                document.Headers.even.InsertParagraph();
                document.Headers.even.InsertTable(t);
                document.Headers.first.InsertParagraph();
                document.Headers.first.InsertTable(t);

                // Insert a Paragraph and a Table into every Footer.
                document.AddFooters();
                document.Footers.odd.InsertParagraph();
                document.Footers.odd.InsertTable(t);
                document.Footers.even.InsertParagraph();
                document.Footers.even.InsertTable(t);
                document.Footers.first.InsertParagraph();
                document.Footers.first.InsertTable(t);

                // Main document tests.
                string document_xml_file = document.mainPart.Uri.OriginalString;
                Assert.IsTrue(document.Paragraphs[0].mainPart.Uri.OriginalString.Equals(document_xml_file));
                Assert.IsTrue(document.Tables[0].mainPart.Uri.OriginalString.Equals(document_xml_file));
                Assert.IsTrue(document.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(document_xml_file));
                Assert.IsTrue(document.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(document_xml_file));
                Assert.IsTrue(document.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(document_xml_file));

                // header first
                Header header_first = document.Headers.first;
                string header_first_xml_file = header_first.mainPart.Uri.OriginalString;

                Assert.IsTrue(header_first.Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));
                Assert.IsTrue(header_first.Tables[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));
                Assert.IsTrue(header_first.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));
                Assert.IsTrue(header_first.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));
                Assert.IsTrue(header_first.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_first_xml_file));

                // header odd
                Header header_odd = document.Headers.odd;
                string header_odd_xml_file = header_odd.mainPart.Uri.OriginalString;

                Assert.IsTrue(header_odd.mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Tables[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));
                Assert.IsTrue(header_odd.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_odd_xml_file));

                // header even
                Header header_even = document.Headers.even;
                string header_even_xml_file = header_even.mainPart.Uri.OriginalString;

                Assert.IsTrue(header_even.mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Tables[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));
                Assert.IsTrue(header_even.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(header_even_xml_file));

                // footer first
                Footer footer_first = document.Footers.first;
                string footer_first_xml_file = footer_first.mainPart.Uri.OriginalString;

                Assert.IsTrue(footer_first.mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Tables[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));
                Assert.IsTrue(footer_first.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_first_xml_file));

                // footer odd
                Footer footer_odd = document.Footers.odd;
                string footer_odd_xml_file = footer_odd.mainPart.Uri.OriginalString;

                Assert.IsTrue(footer_odd.mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Tables[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));
                Assert.IsTrue(footer_odd.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_odd_xml_file));

                // footer even
                Footer footer_even = document.Footers.even;
                string footer_even_xml_file = footer_even.mainPart.Uri.OriginalString;

                Assert.IsTrue(footer_even.mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Tables[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Tables[0].Rows[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Tables[0].Rows[0].Cells[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
                Assert.IsTrue(footer_even.Tables[0].Rows[0].Cells[0].Paragraphs[0].mainPart.Uri.OriginalString.Equals(footer_even_xml_file));
            }
        }

        [Test]
        public void Test_Document_AddImage_FromDisk()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "test_add_images.docx")))
            {
                // Add a png to into this document
                Image png = document.AddImage(Path.Combine(_directoryWithFiles, "purple.png"));
                Assert.IsTrue(document.Images.Count == 1);
                Assert.IsTrue(Path.GetExtension(png.pr.TargetUri.OriginalString) == ".png");

                // Add a tiff into to this document
                Image tif = document.AddImage(Path.Combine(_directoryWithFiles, "yellow.tif"));
                Assert.IsTrue(document.Images.Count == 2);
                Assert.IsTrue(Path.GetExtension(tif.pr.TargetUri.OriginalString) == ".tif");

                // Add a gif into to this document
                Image gif = document.AddImage(Path.Combine(_directoryWithFiles, "orange.gif"));
                Assert.IsTrue(document.Images.Count == 3);
                Assert.IsTrue(Path.GetExtension(gif.pr.TargetUri.OriginalString) == ".gif");

                // Add a jpg into to this document
                Image jpg = document.AddImage(Path.Combine(_directoryWithFiles, "green.jpg"));
                Assert.IsTrue(document.Images.Count == 4);
                Assert.IsTrue(Path.GetExtension(jpg.pr.TargetUri.OriginalString) == ".jpg");

                // Add a bitmap to this document
                Image bitmap = document.AddImage(Path.Combine(_directoryWithFiles, "red.bmp"));
                Assert.IsTrue(document.Images.Count == 5);
                // Word does not allow bmp make sure it was inserted as a png.
                Assert.IsTrue(Path.GetExtension(bitmap.pr.TargetUri.OriginalString) == ".png");
            }
        }

        [Test]
        public void Test_Document_AddImage_FromStream()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "test_add_images.docx")))
            {
                // DocX will always insert Images that come from Streams as jpeg.

                // Add a png to into this document
                Image png = document.AddImage(new FileStream(Path.Combine(_directoryWithFiles, "purple.png"), FileMode.Open));
                Assert.IsTrue(document.Images.Count == 1);
                Assert.IsTrue(Path.GetExtension(png.pr.TargetUri.OriginalString) == ".jpeg");

                // Add a tiff into to this document
                Image tif = document.AddImage(new FileStream(Path.Combine(_directoryWithFiles, "yellow.tif"), FileMode.Open));
                Assert.IsTrue(document.Images.Count == 2);
                Assert.IsTrue(Path.GetExtension(tif.pr.TargetUri.OriginalString) == ".jpeg");

                // Add a gif into to this document
                Image gif = document.AddImage(new FileStream(Path.Combine(_directoryWithFiles, "orange.gif"), FileMode.Open));
                Assert.IsTrue(document.Images.Count == 3);
                Assert.IsTrue(Path.GetExtension(gif.pr.TargetUri.OriginalString) == ".jpeg");

                // Add a jpg into to this document
                Image jpg = document.AddImage(new FileStream(Path.Combine(_directoryWithFiles, "green.jpg"), FileMode.Open));
                Assert.IsTrue(document.Images.Count == 4);
                Assert.IsTrue(Path.GetExtension(jpg.pr.TargetUri.OriginalString) == ".jpeg");

                // Add a bitmap to this document
                Image bitmap = document.AddImage(new FileStream(Path.Combine(_directoryWithFiles, "red.bmp"), FileMode.Open));
                Assert.IsTrue(document.Images.Count == 5);
                // Word does not allow bmp make sure it was inserted as a png.
                Assert.IsTrue(Path.GetExtension(bitmap.pr.TargetUri.OriginalString) == ".jpeg");
            }
        }

        [Test]
        public void Test_Tables()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "Tables.docx")))
            {
                // There is only one Paragraph at the document level.
                Assert.IsTrue(document.Paragraphs.Count() == 13);

                // There is only one Table in this document.
                Assert.IsTrue(document.Tables.Count() == 1);

                // Extract the only Table.
                Table t0 = document.Tables[0];

                // This table has 12 Paragraphs.
                Assert.IsTrue(t0.Paragraphs.Count() == 12);
            }
        }

        [Test]
        public void Test_Images()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "Images.docx")))
            {
                // Extract Images from Document.
                List<Image> document_images = document.Images;

                // Make sure there are 3 Images in this document.
                Assert.IsTrue(document_images.Count() == 3);

                // Extract the headers from this document.
                Headers headers = document.Headers;
                Header header_first = headers.first;
                Header header_odd = headers.odd;
                Header header_even = headers.even;

                #region Header_First
                // Extract Images from the first Header.
                List<Image> header_first_images = header_first.Images;

                // Make sure there is 1 Image in the first header.
                Assert.IsTrue(header_first_images.Count() == 1);
                #endregion

                #region Header_Odd
                // Extract Images from the odd Header.
                List<Image> header_odd_images = header_odd.Images;

                // Make sure there is 1 Image in the first header.
                Assert.IsTrue(header_odd_images.Count() == 1);
                #endregion

                #region Header_Even
                // Extract Images from the odd Header.
                List<Image> header_even_images = header_even.Images;

                // Make sure there is 1 Image in the first header.
                Assert.IsTrue(header_even_images.Count() == 1);
                #endregion
            }
        }

        [Test]
        public void Test_Insert_Picture()
        {
            // Load test document.
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Test.docx")))
            {
                // Add Headers and Footers into this document.
                document.AddHeaders();
                document.AddFooters();
                document.DifferentFirstPage = true;
                document.DifferentOddAndEvenPages = true;

                // Add an Image to this document.
                Image img = document.AddImage(Path.Combine(_directoryWithFiles, "purple.png"));

                // Create a Picture from this Image.
                Picture pic = img.CreatePicture();

                // Main document.
                Paragraph p0 = document.InsertParagraph("Hello");
                p0.InsertPicture(pic, 3);

                // Header first.
                Paragraph p1 = document.Headers.first.InsertParagraph("----");
                p1.InsertPicture(pic, 2);

                // Header odd.
                Paragraph p2 = document.Headers.odd.InsertParagraph("----");
                p2.InsertPicture(pic, 2);

                // Header even.
                Paragraph p3 = document.Headers.even.InsertParagraph("----");
                p3.InsertPicture(pic, 2);

                // Footer first.
                Paragraph p4 = document.Footers.first.InsertParagraph("----");
                p4.InsertPicture(pic, 2);

                // Footer odd.
                Paragraph p5 = document.Footers.odd.InsertParagraph("----");
                p5.InsertPicture(pic, 2);

                // Footer even.
                Paragraph p6 = document.Footers.even.InsertParagraph("----");
                p6.InsertPicture(pic, 2);

                // Save this document.
                document.Save();
            }
        }

		/// <summary>
		/// This test fills two tables with hyperlinks.
		/// </summary>
		[Test]
		public void Test_Insert_Hyperlink_In_Tables()
		{
			using( var input = File.Open( Path.Combine( _directoryWithFiles, "TableSpecifiedHeights.docx" ), FileMode.Open ) )
			{
				using( var doc = DocX.Load( input ) )
				{
					// Make sure content of the file is ok for test
					Assert.IsTrue( doc.Tables.Count > 0 );
					Table tab1 = doc.Tables[ 0 ];
					Assert.IsTrue( tab1.RowCount > 0 );
					Assert.IsTrue( tab1.Rows[0].ColumnCount > 0 );
					doc.InsertParagraph( "" );
					Table tab2 = doc.InsertTable( tab1 );
					Assert.IsTrue( tab2.RowCount > 0 );

					Row row1 = tab1.Rows[ 0 ];
					Row row2 = tab2.Rows[ 0 ];

					// 10 times insert hyperlinks in both tables in tic-tak order
					for( int index = 0; index < 10; index++ )
					{
						Row newRow1 = tab1.InsertRow( row1 );
						Row newRow2 = tab2.InsertRow( row2 );

						Hyperlink h1 = doc.AddHyperlink(
							string.Format( "Table {0}, Row {1}. Google searches for {0} {1}", 1, index + 1 ),
							new Uri( string.Format( "https://www.google.com/search?q=Table{0}Row{1}", 1, index + 1 ) ) );
						newRow1.Cells[ 0 ].Paragraphs[ 0 ].InsertHyperlink( h1 );

						Hyperlink h2 = doc.AddHyperlink(
							string.Format( "Table {0}, Row {1}. Google searches for {0} {1}", 2, index + 1 ),
							new Uri( string.Format( "https://www.google.com/search?q=Table{0}Row{1}", 2, index + 1 ) ) );
						newRow2.Cells[ 0 ].Paragraphs[ 0 ].InsertHyperlink( h2 );

					}
					//Make sure links are ok and in right order
					for( int index = 0; index < doc.Hyperlinks.Count; index++ )
					{
						Hyperlink h = doc.Hyperlinks[ index ];
						string text = string.Format( "Table {0}, Row {1}. Google searches for {0} {1}", ( index / 10 ) + 1, ( index ) % 10 + 1 );
						string uri = string.Format( "https://www.google.com/search?q=Table{0}Row{1}", ( index / 10 ) + 1, ( index ) % 10 + 1 );
						Assert.IsTrue( string.Compare( h.Text, text ) == 0 );
						Assert.IsTrue( h.Uri != null );
						Assert.IsTrue( string.Compare( h.Uri.ToString(), uri ) == 0 );
					}
					doc.SaveAs( Path.Combine( _directoryDocuments, "Test_Insert_Hyperlink_In_Tables.docx" ) );
				}
			}
		}

		/// <summary>
		/// This test makes 2 file. The first uses InsertHyperlink. The second uses AppendHyperlink.
		/// The both hyperlink collections should be equal to each other.
		/// We need be sure the bug in InsertHyperlink is fixed (id attribute in hyperlink was empty and order of inserteed hyperlinks was broken).
		/// </summary>
		[Test]
		public void Test_Compare_InsertHyperlink_And_AppendHyperLinks()
		{
			string fileName1 = Path.Combine( _directoryDocuments, "Test_InsertHyperLinks.docx" );
			string fileName2 = Path.Combine( _directoryDocuments, "Test_AppendHyperlinks.docx" );
			using( DocX document1 = DocX.Create( fileName1 ) )
			{
				using( DocX document2 = DocX.Create( fileName2 ) )
				{
					for( int index = 0; index < 10; index++ )
					{
						Hyperlink h = document1.AddHyperlink(
							string.Format( "Google searches for {0}", index + 1 ),
							new Uri( string.Format( "https://www.google.com/search?q={0}", index + 1 ) ) );
						document1.InsertParagraph( "" ).InsertHyperlink( h );
					}
					document1.Save();

					for( int index = 0; index < 10; index++ )
					{
						Hyperlink h = document2.AddHyperlink(
							string.Format( "Google searches for {0}", index + 1 ),
							new Uri( string.Format( "https://www.google.com/search?q={0}", index + 1 ) ) );
						document2.InsertParagraph( "" ).AppendHyperlink( h );
					}
					document2.Save();

					Assert.IsTrue( document1.Hyperlinks.Count == document2.Hyperlinks.Count );
					for( int index = 0; index < document1.Hyperlinks.Count; index++ )
					{
						Hyperlink h1 = document1.Hyperlinks[ index ];
						Hyperlink h2 = document2.Hyperlinks[ index ];
						Assert.IsTrue( string.Compare( h1.Text, h2.Text ) == 0 );
						Assert.IsTrue( string.Compare( h1.Uri.ToString(), h2.Uri.ToString() ) == 0 );
					}
				}
			}
		}

		[Test]
        public void Test_Insert_Hyperlink()
        {
            // Load test document.
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Test.docx")))
            {
                // Add Headers and Footers into this document.
                document.AddHeaders();
                document.AddFooters();
                document.DifferentFirstPage = true;
                document.DifferentOddAndEvenPages = true;

                // Add a Hyperlink into this document.
                Hyperlink h = document.AddHyperlink("google", new Uri("http://www.google.com"));

                // Main document.
                Paragraph p0 = document.InsertParagraph("Hello");
                p0.InsertHyperlink(h, 3);

                // Header first.
                Paragraph p1 = document.Headers.first.InsertParagraph("----");
                p1.InsertHyperlink(h, 3);

                // Header odd.
                Paragraph p2 = document.Headers.odd.InsertParagraph("----");
                p2.InsertHyperlink(h, 3);

                // Header even.
                Paragraph p3 = document.Headers.even.InsertParagraph("----");
                p3.InsertHyperlink(h, 3);

                // Footer first.
                Paragraph p4 = document.Footers.first.InsertParagraph("----");
                p4.InsertHyperlink(h, 3);

                // Footer odd.
                Paragraph p5 = document.Footers.odd.InsertParagraph("----");
                p5.InsertHyperlink(h, 3);

                // Footer even.
                Paragraph p6 = document.Footers.even.InsertParagraph("----");
                p6.InsertHyperlink(h, 3);

                // Save this document.
                document.Save();
            }
        }

        [Test]
        public void Test_Get_Set_Hyperlink()
        {
            // Load test document.
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "Hyperlinks.docx")))
            {
                // Hyperlinks in the document.
                Assert.IsTrue(document.Hyperlinks.Count == 3);
                Assert.IsTrue(document.Hyperlinks[0].Text == "page1");
                Assert.IsTrue(document.Hyperlinks[0].Uri.AbsoluteUri == "http://www.page1.com/");
                Assert.IsTrue(document.Hyperlinks[1].Text == "page2");
                Assert.IsTrue(document.Hyperlinks[1].Uri.AbsoluteUri == "http://www.page2.com/");
                Assert.IsTrue(document.Hyperlinks[2].Text == "page3");
                Assert.IsTrue(document.Hyperlinks[2].Uri.AbsoluteUri == "http://www.page3.com/");

                // Change the Hyperlinks and check that it has in fact changed.
                document.Hyperlinks[0].Text = "somethingnew";
                document.Hyperlinks[0].Uri = new Uri("http://www.google.com/");
                Assert.IsTrue(document.Hyperlinks[0].Text == "somethingnew");
                Assert.IsTrue(document.Hyperlinks[0].Uri.AbsoluteUri == "http://www.google.com/");
                document.Hyperlinks[1].Text = "somethingnew";
                document.Hyperlinks[1].Uri = new Uri("http://www.google.com/");
                Assert.IsTrue(document.Hyperlinks[1].Text == "somethingnew");
                Assert.IsTrue(document.Hyperlinks[1].Uri.AbsoluteUri == "http://www.google.com/");
                document.Hyperlinks[2].Text = "somethingnew";
                document.Hyperlinks[2].Uri = new Uri("http://www.google.com/");
                Assert.IsTrue(document.Hyperlinks[2].Text == "somethingnew");
                Assert.IsTrue(document.Hyperlinks[2].Uri.AbsoluteUri == "http://www.google.com/");

                Assert.IsTrue(document.Headers.first.Hyperlinks.Count == 1);
                Assert.IsTrue(document.Headers.first.Hyperlinks[0].Text == "header-first");
                Assert.IsTrue(document.Headers.first.Hyperlinks[0].Uri.AbsoluteUri == "http://www.header-first.com/");

                // Change the Hyperlinks and check that it has in fact changed.
                document.Headers.first.Hyperlinks[0].Text = "somethingnew";
                document.Headers.first.Hyperlinks[0].Uri = new Uri("http://www.google.com/");
                Assert.IsTrue(document.Headers.first.Hyperlinks[0].Text == "somethingnew");
                Assert.IsTrue(document.Headers.first.Hyperlinks[0].Uri.AbsoluteUri == "http://www.google.com/");

                Assert.IsTrue(document.Headers.odd.Hyperlinks.Count == 1);
                Assert.IsTrue(document.Headers.odd.Hyperlinks[0].Text == "header-odd");
                Assert.IsTrue(document.Headers.odd.Hyperlinks[0].Uri.AbsoluteUri == "http://www.header-odd.com/");

                // Change the Hyperlinks and check that it has in fact changed.
                document.Headers.odd.Hyperlinks[0].Text = "somethingnew";
                document.Headers.odd.Hyperlinks[0].Uri = new Uri("http://www.google.com/");
                Assert.IsTrue(document.Headers.odd.Hyperlinks[0].Text == "somethingnew");
                Assert.IsTrue(document.Headers.odd.Hyperlinks[0].Uri.AbsoluteUri == "http://www.google.com/");

                Assert.IsTrue(document.Headers.even.Hyperlinks.Count == 1);
                Assert.IsTrue(document.Headers.even.Hyperlinks[0].Text == "header-even");
                Assert.IsTrue(document.Headers.even.Hyperlinks[0].Uri.AbsoluteUri == "http://www.header-even.com/");

                // Change the Hyperlinks and check that it has in fact changed.
                document.Headers.even.Hyperlinks[0].Text = "somethingnew";
                document.Headers.even.Hyperlinks[0].Uri = new Uri("http://www.google.com/");
                Assert.IsTrue(document.Headers.even.Hyperlinks[0].Text == "somethingnew");
                Assert.IsTrue(document.Headers.even.Hyperlinks[0].Uri.AbsoluteUri == "http://www.google.com/");

                Assert.IsTrue(document.Footers.first.Hyperlinks.Count == 1);
                Assert.IsTrue(document.Footers.first.Hyperlinks[0].Text == "footer-first");
                Assert.IsTrue(document.Footers.first.Hyperlinks[0].Uri.AbsoluteUri == "http://www.footer-first.com/");

                // Change the Hyperlinks and check that it has in fact changed.
                document.Footers.first.Hyperlinks[0].Text = "somethingnew";
                document.Footers.first.Hyperlinks[0].Uri = new Uri("http://www.google.com/");
                Assert.IsTrue(document.Footers.first.Hyperlinks[0].Text == "somethingnew");
                Assert.IsTrue(document.Footers.first.Hyperlinks[0].Uri.AbsoluteUri == "http://www.google.com/");

                Assert.IsTrue(document.Footers.odd.Hyperlinks.Count == 1);
                Assert.IsTrue(document.Footers.odd.Hyperlinks[0].Text == "footer-odd");
                Assert.IsTrue(document.Footers.odd.Hyperlinks[0].Uri.AbsoluteUri == "http://www.footer-odd.com/");

                // Change the Hyperlinks and check that it has in fact changed.
                document.Footers.odd.Hyperlinks[0].Text = "somethingnew";
                document.Footers.odd.Hyperlinks[0].Uri = new Uri("http://www.google.com/");
                Assert.IsTrue(document.Footers.odd.Hyperlinks[0].Text == "somethingnew");
                Assert.IsTrue(document.Footers.odd.Hyperlinks[0].Uri.AbsoluteUri == "http://www.google.com/");

                Assert.IsTrue(document.Footers.even.Hyperlinks.Count == 1);
                Assert.IsTrue(document.Footers.even.Hyperlinks[0].Text == "footer-even");
                Assert.IsTrue(document.Footers.even.Hyperlinks[0].Uri.AbsoluteUri == "http://www.footer-even.com/");

                // Change the Hyperlinks and check that it has in fact changed.
                document.Footers.even.Hyperlinks[0].Text = "somethingnew";
                document.Footers.even.Hyperlinks[0].Uri = new Uri("http://www.google.com/");
                Assert.IsTrue(document.Footers.even.Hyperlinks[0].Text == "somethingnew");
                Assert.IsTrue(document.Footers.even.Hyperlinks[0].Uri.AbsoluteUri == "http://www.google.com/");
            }
        }

        [Test]
        public void Test_Append_Hyperlink()
        {
            // Load test document.
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Test.docx")))
            {
                // Add Headers and Footers into this document.
                document.AddHeaders();
                document.AddFooters();
                document.DifferentFirstPage = true;
                document.DifferentOddAndEvenPages = true;

                // Add a Hyperlink to this document.
                Hyperlink h = document.AddHyperlink("google", new Uri("http://www.google.com"));

                // Main document.
                Paragraph p0 = document.InsertParagraph("----");
                p0.AppendHyperlink(h);
                Assert.IsTrue(p0.Text == "----google");

                // Header first.
                Paragraph p1 = document.Headers.first.InsertParagraph("----");
                p1.AppendHyperlink(h);
                Assert.IsTrue(p1.Text == "----google");

                // Header odd.
                Paragraph p2 = document.Headers.odd.InsertParagraph("----");
                p2.AppendHyperlink(h);
                Assert.IsTrue(p2.Text == "----google");

                // Header even.
                Paragraph p3 = document.Headers.even.InsertParagraph("----");
                p3.AppendHyperlink(h);
                Assert.IsTrue(p3.Text == "----google");

                // Footer first.
                Paragraph p4 = document.Footers.first.InsertParagraph("----");
                p4.AppendHyperlink(h);
                Assert.IsTrue(p4.Text == "----google");

                // Footer odd.
                Paragraph p5 = document.Footers.odd.InsertParagraph("----");
                p5.AppendHyperlink(h);
                Assert.IsTrue(p5.Text == "----google");

                // Footer even.
                Paragraph p6 = document.Footers.even.InsertParagraph("----");
                p6.AppendHyperlink(h);
                Assert.IsTrue(p6.Text == "----google");

                // Save the document.
                document.Save();
            }
        }

        [Test]
        public void Test_Append_Picture()
        {
            // Create test document.
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Test.docx")))
            {
                // Add Headers and Footers into this document.
                document.AddHeaders();
                document.AddFooters();
                document.DifferentFirstPage = true;
                document.DifferentOddAndEvenPages = true;

                // Add an Image to this document.
                Image img = document.AddImage(Path.Combine(_directoryWithFiles, "purple.png"));

                // Create a Picture from this Image.
                Picture pic = img.CreatePicture();

                // Main document.
                Paragraph p0 = document.InsertParagraph();
                p0.AppendPicture(pic);

                // Header first.
                Paragraph p1 = document.Headers.first.InsertParagraph();
                p1.AppendPicture(pic);

                // Header odd.
                Paragraph p2 = document.Headers.odd.InsertParagraph();
                p2.AppendPicture(pic);

                // Header even.
                Paragraph p3 = document.Headers.even.InsertParagraph();
                p3.AppendPicture(pic);

                // Footer first.
                Paragraph p4 = document.Footers.first.InsertParagraph();
                p4.AppendPicture(pic);

                // Footer odd.
                Paragraph p5 = document.Footers.odd.InsertParagraph();
                p5.AppendPicture(pic);

                // Footer even.
                Paragraph p6 = document.Footers.even.InsertParagraph();
                p6.AppendPicture(pic);

                // Save the document.
                document.Save();
            }
        }

        [Test]
        public void Test_Move_Picture_Load()
        {
            // Load test document.
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "MovePicture.docx")))
            {
                // Extract the first Picture from the first Paragraph.
                Picture picture = document.Paragraphs.First().Pictures.First();

                // Move it into the first Header.
                Header header_first = document.Headers.first;
                header_first.Paragraphs.First().AppendPicture(picture);

                // Move it into the even Header.
                Header header_even = document.Headers.even;
                header_even.Paragraphs.First().AppendPicture(picture);

                // Move it into the odd Header.
                Header header_odd = document.Headers.odd;
                header_odd.Paragraphs.First().AppendPicture(picture);

                // Move it into the first Footer.
                Footer footer_first = document.Footers.first;
                footer_first.Paragraphs.First().AppendPicture(picture);

                // Move it into the even Footer.
                Footer footer_even = document.Footers.even;
                footer_even.Paragraphs.First().AppendPicture(picture);

                // Move it into the odd Footer.
                Footer footer_odd = document.Footers.odd;
                footer_odd.Paragraphs.First().AppendPicture(picture);

                // Save this as MovedPicture.docx
                document.SaveAs(Path.Combine(_directoryDocuments, "MovedPicture.docx"));
            }
        }

        [Test]
        public void Test_Paragraph_InsertHyperlink()
        {
            // Create a new document
            using (DocX document = DocX.Create("Test.docx"))
            {
                // Add a Hyperlink to this document.
                Hyperlink h = document.AddHyperlink("link", new Uri("http://www.google.com"));

                // Simple
                Paragraph p1 = document.InsertParagraph("AC");
                p1.InsertHyperlink(h); Assert.IsTrue(p1.Text == "linkAC");
                p1.InsertHyperlink(h, p1.Text.Length); Assert.IsTrue(p1.Text == "linkAClink");
                p1.InsertHyperlink(h, p1.Text.IndexOf("C", StringComparison.Ordinal)); Assert.IsTrue(p1.Text == "linkAlinkClink");

                // Difficult
                Paragraph p2 = document.InsertParagraph("\tA\tC\t");
                p2.InsertHyperlink(h); Assert.IsTrue(p2.Text == "link\tA\tC\t");
                p2.InsertHyperlink(h, p2.Text.Length); Assert.IsTrue(p2.Text == "link\tA\tC\tlink");
                p2.InsertHyperlink(h, p2.Text.IndexOf("C", StringComparison.Ordinal)); Assert.IsTrue(p2.Text == "link\tA\tlinkC\tlink");

                // Contrived
                // Add a contrived Hyperlink to this document.
                Hyperlink h2 = document.AddHyperlink("\tlink\t", new Uri("http://www.google.com"));
                Paragraph p3 = document.InsertParagraph("\tA\tC\t");
                p3.InsertHyperlink(h2); Assert.IsTrue(p3.Text == "\tlink\t\tA\tC\t");
                p3.InsertHyperlink(h2, p3.Text.Length); Assert.IsTrue(p3.Text == "\tlink\t\tA\tC\t\tlink\t");
                p3.InsertHyperlink(h2, p3.Text.IndexOf("C", StringComparison.Ordinal)); Assert.IsTrue(p3.Text == "\tlink\t\tA\t\tlink\tC\t\tlink\t");
            }
        }

        [Test]
        public void Test_Paragraph_RemoveHyperlink()
        {
            // Create a new document
            using (DocX document = DocX.Create("Test.docx"))
            {
                // Add a Hyperlink to this document.
                Hyperlink h = document.AddHyperlink("link", new Uri("http://www.google.com"));

                // Simple
                Paragraph p1 = document.InsertParagraph("AC");
                p1.InsertHyperlink(h); Assert.IsTrue(p1.Text == "linkAC");
                p1.InsertHyperlink(h, p1.Text.Length); Assert.IsTrue(p1.Text == "linkAClink");
                p1.InsertHyperlink(h, p1.Text.IndexOf("C", StringComparison.Ordinal)); Assert.IsTrue(p1.Text == "linkAlinkClink");

                // Try and remove a Hyperlink using a negative index.
                // This should throw an exception.
                try
                {
                    p1.RemoveHyperlink(-1);
                    Assert.Fail();
                }
                catch (ArgumentException) { }
                catch (Exception) { Assert.Fail(); }

                // Try and remove a Hyperlink at an index greater than the last.
                // This should throw an exception.
                try
                {
                    p1.RemoveHyperlink(3);
                    Assert.Fail();
                }
                catch (ArgumentException) { }
                catch (Exception) { Assert.Fail(); }

                p1.RemoveHyperlink(0); Assert.IsTrue(p1.Text == "AlinkClink");
                p1.RemoveHyperlink(1); Assert.IsTrue(p1.Text == "AlinkC");
                p1.RemoveHyperlink(0); Assert.IsTrue(p1.Text == "AC");
            }
        }

        [Test]
        public void Test_Paragraph_ReplaceText()
        {
            // Create a new document
            using (DocX document = DocX.Create("Test.docx"))
            {
                // Simple
                Paragraph p1 = document.InsertParagraph("Apple Pear Apple Apple Pear Apple");
                p1.ReplaceText("Apple", "Orange"); Assert.IsTrue(p1.Text == "Orange Pear Orange Orange Pear Orange");
                p1.ReplaceText("Pear", "Apple"); Assert.IsTrue(p1.Text == "Orange Apple Orange Orange Apple Orange");
                p1.ReplaceText("Orange", "Pear"); Assert.IsTrue(p1.Text == "Pear Apple Pear Pear Apple Pear");

                // Try and replace text that dosen't exist in the Paragraph.
                string old = p1.Text;
                p1.ReplaceText("foo", "bar"); Assert.IsTrue(p1.Text.Equals(old));

                // Difficult
                Paragraph p2 = document.InsertParagraph("Apple Pear Apple Apple Pear Apple");
                p2.ReplaceText(" ", "\t"); Assert.IsTrue(p2.Text == "Apple\tPear\tApple\tApple\tPear\tApple");
                p2.ReplaceText("\tApple\tApple", ""); Assert.IsTrue(p2.Text == "Apple\tPear\tPear\tApple");
                p2.ReplaceText("Apple\tPear\t", ""); Assert.IsTrue(p2.Text == "Pear\tApple");
                p2.ReplaceText("Pear\tApple", ""); Assert.IsTrue(p2.Text == "");
            }
        }

        [Test]
        public void Test_Paragraph_ReplaceTextInGivenFormat()
        {
            // Load a document.
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "VariousTextFormatting.docx")))
            {
                // Removing red text highlighted with yellow
                var formatting = new DocXFormatting();
                formatting.FontColor = WindowsColor.Blue;
                // IMPORTANT: default constructor of Formatting sets up language property - set it to NULL to be language independent
                var desiredFormat = new DocXFormatting() { Language = null, FontColor = WindowsColor.FromArgb(255, 0, 0), Highlight = Highlight.yellow };
                var replaced = string.Empty;
                foreach (var p in document.Paragraphs)
                {
                    if (p.Text == "Text highlighted with yellow")
                    {
                        p.ReplaceText("Text highlighted with yellow", "New text highlighted with yellow", false, RegexOptions.None, null, desiredFormat, MatchFormattingOptions.ExactMatch);
                        replaced += p.Text;
                    }
                }

                Assert.AreEqual("New text highlighted with yellow", replaced);

                // Removing red text with no other formatting (ExactMatch)
                desiredFormat = new DocXFormatting() { Language = null, FontColor = WindowsColor.FromArgb(255, 0, 0) };
                var count = 0;

                foreach (var p in document.Paragraphs)
                {
                    p.ReplaceText("Text", "Replaced text", false, RegexOptions.None, null, desiredFormat, MatchFormattingOptions.ExactMatch);

                    if (p.Text.StartsWith("Replaced text"))
                    {
                        ++count;
                    }
                }

                Assert.AreEqual(1, count);

                // Removing just red text with any other formatting (SubsetMatch)
                desiredFormat = new DocXFormatting() { Language = null, FontColor = WindowsColor.FromArgb(255, 0, 0) };
                count = 0;
                foreach (var p in document.Paragraphs)
                {
                    p.ReplaceText("Text", "Replaced text", false, RegexOptions.None, null, desiredFormat);
                    if (p.Text.StartsWith("Replaced text"))
                    {
                        ++count;
                    }
                }

                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void Test_Paragraph_RemoveText()
        {
            // Create a new document
            using (DocX document = DocX.Create("Test.docx"))
            {
                // Simple
                //<p>
                //    <r><t>HellWorld</t></r>
                //</p>
                Paragraph p1 = document.InsertParagraph("HelloWorld");
                p1.RemoveText(0, 1); Assert.IsTrue(p1.Text == "elloWorld");
                p1.RemoveText(p1.Text.Length - 1, 1); Assert.IsTrue(p1.Text == "elloWorl");
                p1.RemoveText(p1.Text.IndexOf("o", StringComparison.Ordinal), 1); Assert.IsTrue(p1.Text == "ellWorl");

                // Try and remove text at an index greater than the last.
                // This should throw an exception.
                try
                {
                    p1.RemoveText(p1.Text.Length, 1);
                    Assert.Fail();
                }
                catch (ArgumentOutOfRangeException) { }
                catch (Exception) { Assert.Fail(); }

                // Try and remove text at a negative index.
                // This should throw an exception.
                try
                {
                    p1.RemoveText(-1, 1);
                    Assert.Fail();
                }
                catch (ArgumentOutOfRangeException) { }
                catch (Exception) { Assert.Fail(); }

                // Difficult
                //<p>
                //    <r><t>A</t></r>
                //    <r><t>B</t></r>
                //    <r><t>C</t></r>
                //</p>
                Paragraph p2 = document.InsertParagraph("A\tB\tC");
                p2.RemoveText(0, 1); Assert.IsTrue(p2.Text == "\tB\tC");
                p2.RemoveText(p2.Text.Length - 1, 1); Assert.IsTrue(p2.Text == "\tB\t");
                p2.RemoveText(p2.Text.IndexOf("B", StringComparison.Ordinal), 1); Assert.IsTrue(p2.Text == "\t\t");
                p2.RemoveText(0, 1); Assert.IsTrue(p2.Text == "\t");
                p2.RemoveText(0, 1); Assert.IsTrue(p2.Text == "");

                // Contrived 1
                //<p>
                //    <r>
                //        <t>A</t>
                //        <t>B</t>
                //        <t>C</t>
                //    </r>
                //</p>
                Paragraph p3 = document.InsertParagraph("");
                p3.Xml = XElement.Parse
                (
                    @"<w:p xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>
                        <w:pPr />
                        <w:r>
                            <w:rPr />
                            <w:t>A</w:t>
                            <w:t>B</w:t>
                            <w:t>C</w:t>
                        </w:r>
                    </w:p>"
                );

                p3.RemoveText(0, 1); Assert.IsTrue(p3.Text == "BC");
                p3.RemoveText(p3.Text.Length - 1, 1); Assert.IsTrue(p3.Text == "B");
                p3.RemoveText(0, 1); Assert.IsTrue(p3.Text == "");

                // Contrived 2
                //<p>
                //    <r>
                //        <t>A</t>
                //        <t>B</t>
                //        <t>C</t>
                //    </r>
                //</p>
                Paragraph p4 = document.InsertParagraph("");
                p4.Xml = XElement.Parse
                (
                    @"<w:p xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>
                        <w:pPr />
                        <w:r>
                            <w:rPr />
                            <tab />
                            <w:t>A</w:t>
                            <tab />
                        </w:r>
                        <w:r>
                            <w:rPr />
                            <tab />
                            <w:t>B</w:t>
                            <tab />
                        </w:r>
                    </w:p>"
                );

                p4.RemoveText(0, 1); Assert.IsTrue(p4.Text == "A\t\tB\t");
                p4.RemoveText(1, 1); Assert.IsTrue(p4.Text == "A\tB\t");
                p4.RemoveText(p4.Text.Length - 1, 1); Assert.IsTrue(p4.Text == "A\tB");
                p4.RemoveText(1, 1); Assert.IsTrue(p4.Text == "AB");
                p4.RemoveText(p4.Text.Length - 1, 1); Assert.IsTrue(p4.Text == "A");
                p4.RemoveText(p4.Text.Length - 1, 1); Assert.IsTrue(p4.Text == "");

				// Checks for parameter removeEmptyParagraph
				int originalParagraphCount = document.Paragraphs.Count;
				string paraToDelText = "text to delete";

				Paragraph paraToDel = document.InsertParagraph( paraToDelText );
				Assert.IsTrue( document.Paragraphs.Count == originalParagraphCount + 1 );
				// Remove text with paragraph
				paraToDel.RemoveText( 0, paraToDelText.Length, false, true );
				Assert.IsTrue( document.Paragraphs.Count == originalParagraphCount );
				originalParagraphCount = document.Paragraphs.Count;

				paraToDel = document.InsertParagraph( paraToDelText );
				Assert.IsTrue( document.Paragraphs.Count == originalParagraphCount + 1 );
				// Remove text and keep paragraph
				paraToDel.RemoveText( 0, paraToDelText.Length, false, false );
				Assert.IsTrue( document.Paragraphs.Count == originalParagraphCount + 1 );
			}
		}

        [Test]
        public void Test_Document_RemoveTextInGivenFormat()
        {
            // Load a document.
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "VariousTextFormatting.docx")))
            {
                var formatting = new DocXFormatting();
                formatting.FontColor = WindowsColor.Blue;
                // IMPORTANT: default constructor of Formatting sets up language property - set it to NULL to be language independent
                formatting.Language = null;
                var deletedCount = document.RemoveTextInGivenFormat(formatting);
                Assert.AreEqual(2, deletedCount);

                deletedCount = document.RemoveTextInGivenFormat(new DocXFormatting() { Highlight = Highlight.yellow, Language = null });
                Assert.AreEqual(2, deletedCount);

                deletedCount = document.RemoveTextInGivenFormat(new DocXFormatting() { Highlight = Highlight.blue, Language = null, FontColor = WindowsColor.FromArgb(0, 255, 0) });
                Assert.AreEqual(1, deletedCount);

                deletedCount = document.RemoveTextInGivenFormat(new DocXFormatting() { Language = null, FontColor = WindowsColor.FromArgb(123, 123, 123) }, MatchFormattingOptions.ExactMatch);
                Assert.AreEqual(2, deletedCount);
            }
        }

        [Test]
        public void Test_Paragraph_InsertText()
        {
            // Create a new document
            using (DocX document = DocX.Create("Test.docx"))
            {
                // Simple
                //<p>
                //    <r><t>HelloWorld</t></r>
                //</p>
                Paragraph p1 = document.InsertParagraph("HelloWorld");
                p1.InsertText(0, "-"); Assert.IsTrue(p1.Text == "-HelloWorld");
                p1.InsertText(p1.Text.Length, "-"); Assert.IsTrue(p1.Text == "-HelloWorld-");
                p1.InsertText(p1.Text.IndexOf("W", StringComparison.Ordinal), "-"); Assert.IsTrue(p1.Text == "-Hello-World-");

                // Try and insert text at an index greater than the last + 1.
                // This should throw an exception.
                try
                {
                    p1.InsertText(p1.Text.Length + 1, "-");
                    Assert.Fail();
                }
                catch (ArgumentOutOfRangeException) { }
                catch (Exception) { Assert.Fail(); }

                // Try and insert text at a negative index.
                // This should throw an exception.
                try
                {
                    p1.InsertText(-1, "-");
                    Assert.Fail();
                }
                catch (ArgumentOutOfRangeException) { }
                catch (Exception) { Assert.Fail(); }

                // Difficult
                //<p>
                //    <r><t>A</t></r>
                //    <r><t>B</t></r>
                //    <r><t>C</t></r>
                //</p>
                Paragraph p2 = document.InsertParagraph("A\tB\tC");
                p2.InsertText(0, "-"); Assert.IsTrue(p2.Text == "-A\tB\tC");
                p2.InsertText(p2.Text.Length, "-"); Assert.IsTrue(p2.Text == "-A\tB\tC-");
                p2.InsertText(p2.Text.IndexOf("B", StringComparison.Ordinal), "-"); Assert.IsTrue(p2.Text == "-A\t-B\tC-");
                p2.InsertText(p2.Text.IndexOf("C", StringComparison.Ordinal), "-"); Assert.IsTrue(p2.Text == "-A\t-B\t-C-");

                // Contrived 1
                //<p>
                //    <r>
                //        <t>A</t>
                //        <t>B</t>
                //        <t>C</t>
                //    </r>
                //</p>
                Paragraph p3 = document.InsertParagraph("");
                p3.Xml = XElement.Parse
                (
                    @"<w:p xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>
                        <w:pPr />
                        <w:r>
                            <w:rPr />
                            <w:t>A</w:t>
                            <w:t>B</w:t>
                            <w:t>C</w:t>
                        </w:r>
                    </w:p>"
                );

                p3.InsertText(0, "-"); Assert.IsTrue(p3.Text == "-ABC");
                p3.InsertText(p3.Text.Length, "-"); Assert.IsTrue(p3.Text == "-ABC-");
                p3.InsertText(p3.Text.IndexOf("B", StringComparison.Ordinal), "-"); Assert.IsTrue(p3.Text == "-A-BC-");
                p3.InsertText(p3.Text.IndexOf("C", StringComparison.Ordinal), "-"); Assert.IsTrue(p3.Text == "-A-B-C-");

                // Contrived 2
                //<p>
                //    <r>
                //        <t>A</t>
                //        <t>B</t>
                //        <t>C</t>
                //    </r>
                //</p>
                Paragraph p4 = document.InsertParagraph("");
                p4.Xml = XElement.Parse
                (
                    @"<w:p xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>
                        <w:pPr />
                        <w:r>
                            <w:rPr />
                            <w:t>A</w:t>
                            <w:t>B</w:t>
                            <w:t>C</w:t>
                        </w:r>
                    </w:p>"
                );

                p4.InsertText(0, "\t"); Assert.IsTrue(p4.Text == "\tABC");
                p4.InsertText(p4.Text.Length, "\t"); Assert.IsTrue(p4.Text == "\tABC\t");
                p4.InsertText(p4.Text.IndexOf("B", StringComparison.Ordinal), "\t"); Assert.IsTrue(p4.Text == "\tA\tBC\t");
                p4.InsertText(p4.Text.IndexOf("C", StringComparison.Ordinal), "\t"); Assert.IsTrue(p4.Text == "\tA\tB\tC\t");
            }
        }

        [Test]
        public void Test_Document_Paragraphs()
        {
            string temporaryFilePath = Path.Combine(_directoryDocuments, "temp.docx");

            // Load the document 'Paragraphs.docx'
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "Paragraphs.docx")))
            {
                // Extract the Paragraphs from this document.
                ReadOnlyCollection<Paragraph> paragraphs = document.Paragraphs;

                // There should be 3 Paragraphs in this document.
                Assert.IsTrue(paragraphs.Count() == 3);

                // Extract the 3 Paragraphs.
                Paragraph p1 = paragraphs[0];
                Paragraph p2 = paragraphs[1];
                Paragraph p3 = paragraphs[2];

                // Extract their Text properties.
                string p1_text = p1.Text;
                string p2_text = p2.Text;
                string p3_text = p3.Text;

                // Test their Text properties against absolutes.
                Assert.IsTrue(p1_text == "Paragraph 1");
                Assert.IsTrue(p2_text == "Paragraph 2");
                Assert.IsTrue(p3_text == "Paragraph 3");

                // Its important that each Paragraph knows the PackagePart it belongs to.
                foreach (var paragraph in document.Paragraphs)
                {
                    Assert.IsTrue(paragraph.PackagePart.Uri.ToString() == package_part_document);
                }

                // Test the saving of the document.
                document.SaveAs(temporaryFilePath);
            }

            // Delete the tempory file.
            File.Delete(temporaryFilePath);
        }

        [Test]
        public void Test_Table_mainPart_bug9526()
        {
            using (DocX document = DocX.Create("test.docx"))
            {
                Hyperlink h = document.AddHyperlink("follow me", new Uri("http://www.google.com"));
                Table t = document.AddTable(2, 3);
                int cc = t.ColumnCount;

                Paragraph p = t.Rows[0].Cells[0].Paragraphs[0];
                p.AppendHyperlink(h);
            }
        }

        [Test]
        public void Test_Table_RemoveColumnWithMergedCells()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Tables3.docx")))
            {
                //Add A table                
                Table t = document.AddTable(2, 3);
                t.Design = TableDesign.TableGrid;

                Table t1 = document.InsertTable(t);
                t1.Rows[0].MergeCells(1, 2);
                t1.RemoveColumn();
                document.Save();
            }
        }

        [Test]
        public void Test_Table_MergedRowCellsMergedWithColumnMergedCells()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Tables3.docx")))
            {
                //Add A table                
                Table t = document.AddTable(3, 3);
                t.Design = TableDesign.TableGrid;

                Table t1 = document.InsertTable(t);
                t1.Rows[0].MergeCells(0, 1);
                t1.Rows[1].MergeCells(0, 1);
                t1.MergeCellsInColumn(0, 0, 1);
                document.Save();
            }
        }
        
        [Test]
        public void Test_Table_RemoveRowWithMergedCells()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Tables3.docx")))
            {
                //Add A table                
                Table t = document.AddTable(3, 4);
                t.Design = TableDesign.TableGrid;

                Table t1 = document.InsertTable(t);
                t1.Rows[0].MergeCells(1, 2);
                t1.RemoveRow();
                t1.MergeCellsInColumn(0, 0, 1);
                t1.InsertRow();
                t1.RemoveRow(1);
                document.Save();
            }
        }

        [Test]
        public void Test_Table_InsertRow()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Tables3.docx")))
            {
                //Add A table                
                Table t = document.AddTable(2, 3);
                t.Design = TableDesign.TableGrid;

                Table t1 = document.InsertTable(t);
                t1.MergeCellsInColumn(1, 0, 1);
                t1.InsertRow();
                t1.Rows[2].MergeCells(1, 3);
                t1.InsertRow(3);
                document.Save();
            }
        }

        [Test]
        public void Test_Table_AddColumnWithCellDeleted()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Tables3.docx")))
            {
                //Add A table                
                Table t = document.AddTable(2, 3);
                t.Design = TableDesign.TableGrid;

                Table t1 = document.InsertTable(t);

                t1.DeleteAndShiftCellsLeft(0, 1);
                // 4 columns
                t1.InsertColumn();
                t1.DeleteAndShiftCellsLeft(0, 1);
                // 5 columns
                t1.InsertColumn();
                t1.DeleteAndShiftCellsLeft(0, 1);

                document.Save();            
            }
        }

        [Test]
        public void Test_Table_DeleteCellInRow()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Tables3.docx")))
            {
                //Add A table                
                Table t = document.AddTable(2, 2);
                t.Design = TableDesign.TableGrid;

                Table t1 = document.InsertTable(t);

                t1.DeleteAndShiftCellsLeft(0, 1);
                document.Save();
                document.Save();
            }
        }

        [Test]
        public void Test_Table_InsertColumnWithMergedCells()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Tables3.docx")))
            {
                //Add A table                
                Table t = document.AddTable(2, 2);
                t.Design = TableDesign.TableGrid;

                Table t1 = document.InsertTable(t);

                t1.InsertColumn(2, true);
                t1.InsertColumn(2, true);
                t1.InsertColumn(2, true);
                t1.InsertColumn(2, true);
                t1.Rows[0].MergeCells(1, 4);

                Assert.AreEqual(t1.Rows[1].Cells.Count, 6);
                Assert.AreEqual(t1.ColumnCount, 6);

                foreach (Row r in t1.Rows)
                {
                    foreach (Cell c in r.Cells)
                    {
                        c.Paragraphs[0].InsertText("Hello");
                    }
                }
                t1.InsertColumn(6, false);           
                t1.InsertColumn();
                t1.InsertColumn(3, true);
                t1.InsertColumn(6, true);
                t1.InsertColumn(6, true);
                t1.InsertColumn(5, true);
                document.Save();
            }
        }

        [Test]
        public void Test_Table_InsertRowAndColumn()
        {
            // Create a table
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "Tables2.docx")))
            {
                // Add a Table to a document.
                Table t = document.AddTable(2, 2);
                t.Design = TableDesign.TableGrid;

                t.Rows[0].Cells[0].Paragraphs[0].InsertText("X");
                t.Rows[0].Cells[1].Paragraphs[0].InsertText("X");
                t.Rows[1].Cells[0].Paragraphs[0].InsertText("X");
                t.Rows[1].Cells[1].Paragraphs[0].InsertText("X");

                // Insert the Table into the main section of the document.
                Table t1 = document.InsertTable(t);
                // ... and add a column and a row
                t1.InsertRow(1);
                t1.InsertColumn(1, true);

                // Save the document.
                document.Save();
            }

            // Check table
            using (DocX document = DocX.Load(Path.Combine(_directoryDocuments, "Tables2.docx")))
            {
                // Get a table from a document
                Table t = document.Tables[0];

                // Check that the table is equal this: 
                // X - X
                // - - -
                // X - X
                Assert.AreEqual("X", t.Rows[0].Cells[0].Paragraphs[0].Text);
                Assert.AreEqual("X", t.Rows[2].Cells[0].Paragraphs[0].Text);
                Assert.AreEqual("X", t.Rows[0].Cells[2].Paragraphs[0].Text);
                Assert.AreEqual("X", t.Rows[2].Cells[2].Paragraphs[0].Text);
                Assert.IsTrue(String.IsNullOrEmpty(t.Rows[1].Cells[0].Paragraphs[0].Text));
                Assert.IsTrue(String.IsNullOrEmpty(t.Rows[1].Cells[1].Paragraphs[0].Text));
                Assert.IsTrue(String.IsNullOrEmpty(t.Rows[1].Cells[2].Paragraphs[0].Text));
                Assert.IsTrue(String.IsNullOrEmpty(t.Rows[0].Cells[1].Paragraphs[0].Text));
                Assert.IsTrue(String.IsNullOrEmpty(t.Rows[2].Cells[1].Paragraphs[0].Text));
            }
        }

        [Test]
        public void Test_Table_SetTableDesignNone()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "TablesDesign.docx")))
            {
                //Add A table                
                Table t = document.AddTable(2, 3);
                Table t1 = document.InsertTable(t);
                t1.Design = TableDesign.None;
                // requires FIX for .None/.Custom set in a row
              //  t1.Design = TableDesign.None;
              //  t1.Design = TableDesign.Custom;
                t1.Design = TableDesign.Custom;
                t1.Design = TableDesign.ColorfulGrid;
                t1.Design = TableDesign.ColorfulGrid;
                t1.MergeCellsInColumn(1, 0, 1);
                t1.InsertRow();
                t1.Rows[2].MergeCells(1, 3);
                t1.InsertRow(3);
                document.Save();
            }
        }

        [Test]
        public void Test_Document_ApplyTemplate()
        {
            using (MemoryStream documentStream = new MemoryStream())
            {
                using (DocX document = DocX.Create(documentStream))
                {
                    document.ApplyTemplate(Path.Combine(_directoryWithFiles, "Template.dotx"));
                    document.Save();

                    Header firstHeader = document.Headers.first;
                    Header oddHeader = document.Headers.odd;
                    Header evenHeader = document.Headers.even;

                    Footer firstFooter = document.Footers.first;
                    Footer oddFooter = document.Footers.odd;
                    Footer evenFooter = document.Footers.even;

                    Assert.IsTrue(firstHeader.Paragraphs.Count == 1, "More than one paragraph in header.");
                    Assert.IsTrue(firstHeader.Paragraphs[0].Text.Equals("First page header"), "Header isn't retrieved from template.");

                    Assert.IsTrue(oddHeader.Paragraphs.Count == 1, "More than one paragraph in header.");
                    Assert.IsTrue(oddHeader.Paragraphs[0].Text.Equals("Odd page header"), "Header isn't retrieved from template.");

                    Assert.IsTrue(evenHeader.Paragraphs.Count == 1, "More than one paragraph in header.");
                    Assert.IsTrue(evenHeader.Paragraphs[0].Text.Equals("Even page header"), "Header isn't retrieved from template.");

                    Assert.IsTrue(firstFooter.Paragraphs.Count == 1, "More than one paragraph in footer.");
                    Assert.IsTrue(firstFooter.Paragraphs[0].Text.Equals("First page footer"), "Footer isn't retrieved from template.");

                    Assert.IsTrue(oddFooter.Paragraphs.Count == 1, "More than one paragraph in footer.");
                    Assert.IsTrue(oddFooter.Paragraphs[0].Text.Equals("Odd page footer"), "Footer isn't retrieved from template.");

                    Assert.IsTrue(evenFooter.Paragraphs.Count == 1, "More than one paragraph in footer.");
                    Assert.IsTrue(evenFooter.Paragraphs[0].Text.Equals("Even page footer"), "Footer isn't retrieved from template.");

                    Paragraph firstParagraph = document.Paragraphs[0];
                    Assert.IsTrue(firstParagraph.StyleName.Equals("DocXSample"), "First paragraph isn't of style from template.");
                }
            }
        }
        [Test]
        public void When_opening_a_template_no_error_should_be_thrown()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "Template.dotx")))
            {
                Assert.IsTrue(document.Paragraphs.Count > 0);
            }
        }
        [Test]
        public void Saving_and_loading_a_template_should_work()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "test template.dotx"), DocumentTypes.Template))
            {
                document.InsertParagraph("hello, this is a paragraph");
                document.Save();
            }
            using (DocX document = DocX.Load(Path.Combine(_directoryDocuments, "test template.dotx")))
            {
                Assert.IsTrue(document.Paragraphs.Count > 0);
            }

        }
        [Test]
        public void Test_ParentContainer_When_Creating_Doc()
        {
            using (DocX document = DocX.Create("Test.docx"))
            {
                document.AddHeaders();
                Paragraph p1 = document.Headers.first.InsertParagraph("Test");

                Assert.IsTrue(p1.ParentContainer == ContainerType.Header);
            }
        }

        [Test]
        public void Test_Section_Count_When_Creating_Doc()
        {
            //This adds a section break - so insert paragraphs, and follow it up by a section break/paragraph
            using (DocX document = DocX.Create("TestSectionCount.docx"))
            {
                document.InsertSection();

                var sections = document.GetSections();

                Assert.AreEqual(sections.Count(), 2);
            }

        }

        [Test]
        public void Test_Sections_And_Paragraphs_When_Creating_Doc()
        {
            //This adds a section break - so insert paragraphs, and follow it up by a section break/paragraph
            using (DocX document = DocX.Create("TestSectionAndParagraph.docx"))
            {
                //Add 2 paras and a break
                document.InsertParagraph("First Para");
                document.InsertParagraph("Second Para");
                document.InsertSection();
                document.InsertParagraph("This is default para");

                var sections = document.GetSections();

                Assert.AreEqual(sections.Count(), 2);
            }


        }

        [Test]
        public void Test_ParentContainer_When_Reading_Doc()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "Tables.docx")))
            {
                ReadOnlyCollection<Paragraph> paragraphs = document.Paragraphs;

                Paragraph p1 = paragraphs[0];

                Assert.IsTrue(p1.ParentContainer == ContainerType.Cell);
            }

        }

        [Test]
        public void Test_Section_Count_When_Reading_Doc()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "testdoc_SectionsWithSectionBreaks.docx")))
            {
                var sections = document.GetSections();

                Assert.AreEqual(sections.Count(), 4);
            }

        }

        [Test]
        public void Test_Section_Paragraph_Count_Match_When_Reading_Doc()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "testdoc_SectionsWithSectionBreaksMultiParagraph.docx")))
            {

                var sections = document.GetSections();

                Assert.AreEqual(sections[0].SectionParagraphs.Count, 2);
                Assert.AreEqual(sections[1].SectionParagraphs.Count, 1);
                Assert.AreEqual(sections[2].SectionParagraphs.Count, 2);
                Assert.AreEqual(sections[3].SectionParagraphs.Count, 1);
            }
        }

        [Test]
        public void Test_Section_Paragraph_Content_Match_When_Reading_Doc()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "testdoc_SectionsWithSectionBreaks.docx")))
            {

                var sections = document.GetSections();

                Assert.IsTrue(sections[0].SectionParagraphs[0].Text.Contains("Section 1"));
                Assert.IsTrue(sections[1].SectionParagraphs[0].Text.Contains("Section 2"));
                Assert.IsTrue(sections[2].SectionParagraphs[0].Text.Contains("Section 3"));
                Assert.IsTrue(sections[3].SectionParagraphs[0].Text.Contains("Section 4"));

            }
        }

        [Test]
        public void Test_Ordered_List_When_Reading_Doc()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "testdoc_OrderedList.docx")))
            {

                var sections = document.GetSections();

                Assert.IsTrue(sections[0].SectionParagraphs[0].IsListItem);
                Assert.IsTrue(sections[0].SectionParagraphs[1].IsListItem);
                Assert.IsTrue(sections[0].SectionParagraphs[2].IsListItem);

                Assert.AreEqual(sections[0].SectionParagraphs[0].ListItemType, ListItemType.Numbered);
                Assert.AreEqual(sections[0].SectionParagraphs[1].ListItemType, ListItemType.Numbered);
                Assert.AreEqual(sections[0].SectionParagraphs[2].ListItemType, ListItemType.Numbered);
            }
        }

        [Test]
        public void Test_Unordered_List_When_Reading_Doc()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "testdoc_UnorderedList.docx")))
            {

                var sections = document.GetSections();

                Assert.IsTrue(sections[0].SectionParagraphs[0].IsListItem);
                Assert.IsTrue(sections[0].SectionParagraphs[1].IsListItem);
                Assert.IsTrue(sections[0].SectionParagraphs[2].IsListItem);

                Assert.AreEqual(sections[0].SectionParagraphs[0].ListItemType, ListItemType.Bulleted);
                Assert.AreEqual(sections[0].SectionParagraphs[1].ListItemType, ListItemType.Bulleted);
                Assert.AreEqual(sections[0].SectionParagraphs[2].ListItemType, ListItemType.Bulleted);
            }
        }

        [Test]
        public void Test_Ordered_Unordered_Lists_When_Reading_Doc()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "testdoc_OrderedUnorderedLists.docx")))
            {

                var sections = document.GetSections();

                Assert.IsTrue(sections[0].SectionParagraphs[0].IsListItem);
                Assert.IsTrue(sections[0].SectionParagraphs[1].IsListItem);
                Assert.IsTrue(sections[0].SectionParagraphs[2].IsListItem);

                Assert.AreEqual(sections[0].SectionParagraphs[0].ListItemType, ListItemType.Numbered);
                Assert.AreEqual(sections[0].SectionParagraphs[1].ListItemType, ListItemType.Numbered);
                Assert.AreEqual(sections[0].SectionParagraphs[2].ListItemType, ListItemType.Numbered);

                Assert.IsTrue(sections[0].SectionParagraphs[3].IsListItem);
                Assert.IsTrue(sections[0].SectionParagraphs[4].IsListItem);
                Assert.IsTrue(sections[0].SectionParagraphs[5].IsListItem);

                Assert.AreEqual(sections[0].SectionParagraphs[3].ListItemType, ListItemType.Bulleted);
                Assert.AreEqual(sections[0].SectionParagraphs[4].ListItemType, ListItemType.Bulleted);
                Assert.AreEqual(sections[0].SectionParagraphs[5].ListItemType, ListItemType.Bulleted);

            }
        }

        [Test]
        public void WhenCreatingAnOrderedListTheListXmlShouldHaveNumberedListItemType()
        {

            using (DocX document = DocX.Create("TestListXmlNumbered.docx"))
            {
                const int level = 0;
                XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                var list = document.AddList("First Item");
                document.InsertList(list);

                var listNumPropNode = document.mainDoc.Descendants().First(s => s.Name.LocalName == "numPr");

                var numId = listNumPropNode.Descendants().First(s => s.Name.LocalName == "numId");
                var abstractNum = list.GetAbstractNum(int.Parse(numId.GetAttribute(w + "val")));
                var lvl = abstractNum.Descendants().First(d => d.Name.LocalName == "lvl" && d.GetAttribute(w + "ilvl").Equals(level.ToString()));
                var numFormat = lvl.Descendants().First(d => d.Name.LocalName == "numFmt");

                Assert.AreEqual(numFormat.GetAttribute(w + "val").ToLower(), "decimal");
            }

        }

        [Test]
        public void WhenCreatingAnUnOrderedListTheListXmlShouldHaveBulletListItemType()
        {

            using (DocX document = DocX.Create("TestListXmlBullet.docx"))
            {
                var list = document.AddList("First Item", 0, ListItemType.Bulleted);
                document.InsertList(list);

                var listNumPropNode = document.mainDoc.Descendants().First(s => s.Name.LocalName == "numPr");

                var numId = listNumPropNode.Descendants().First(s => s.Name.LocalName == "numId");

                Assert.AreEqual(numId.Attribute(DocX.w + "val").Value, "1");
            }
        }

        [Test]
        public void WhenCreatingAListWithTextTheListXmlShouldHaveTheCorrectRunItemText()
        {
            using (DocX document = DocX.Create("TestListCreate.docx"))
            {
                const string listText = "RunText";
                var list = document.AddList(listText, 0, ListItemType.Bulleted);
                document.InsertList(list);

                var listNumPropNode = document.mainDoc.Descendants().First(s => s.Name.LocalName == "numPr");

                var runTextNode = document.mainDoc.Descendants().First(s => s.Name.LocalName == "t");

                Assert.IsNotNull(listNumPropNode);
                Assert.AreEqual(list.Items.First().runs.First().Value, runTextNode.Value);
                Assert.AreEqual(listText, runTextNode.Value);
            }
        }

        [Test]
        public void WhenCreatingAnOrderedListTheListShouldHaveNumberedListItemType()
        {

            using (DocX document = DocX.Create("TestListCreateOrderedList.docx"))
            {
                var list = document.AddList("First Item");

                Assert.AreEqual(list.ListType, ListItemType.Numbered);
            }

        }

        [Test]
        public void WhenCreatingAnUnOrderedListTheListShouldHaveBulletListItemType()
        {

            using (DocX document = DocX.Create("TestListCreateUnorderedList.docx"))
            {
                var list = document.AddList("First Item", 0, ListItemType.Bulleted);

                Assert.AreEqual(list.ListType, ListItemType.Bulleted);
            }

        }

        [Test]
        public void WhenCreatingAListWithTextTheListShouldHaveTheCorrectRunItemText()
        {

            using (DocX document = DocX.Create("TestListCreateRunText.docx"))
            {
                var list = document.AddList("RunText", 0, ListItemType.Bulleted);
                document.InsertList(list);

                Assert.AreEqual(list.Items.First().runs.First().Value, "RunText");
            }
        }
        
        [Test]
        public void WhenCreatingAListTheListStyleShouldExistOrBeCreated()
        {

            using (DocX document = DocX.Create("TestListStyle.docx"))
            {
                var style = document.AddStylesForList();

                XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

                bool listStyleExists =
                    (
                      from s in style.Element(w + "styles").Elements()
                      let styleId = s.Attribute(XName.Get("styleId", w.NamespaceName))
                      where (styleId != null && styleId.Value == "ListParagraph")
                      select s
                    ).Any();

                Assert.IsTrue(listStyleExists);

            }
        }

        [Test]
        public void ANewListItemShouldCreateAnAbstractNumberingEntry()
        {
            using (DocX document = DocX.Create("TestNumbering.docx"))
            {
                var numbering = document.numbering.Descendants().Where(d => d.Name.LocalName == "abstractNum");
                Assert.IsFalse(numbering.Any());

                document.AddList("List Text");

                numbering = document.numbering.Descendants().Where(d => d.Name.LocalName == "abstractNum");
                Assert.IsTrue(numbering.Any());
            }
        }

        [Test]
        public void ANewListItemShouldCreateANewNumEntry()
        {
            using (DocX document = DocX.Create("TestNumEntry.docx"))
            {
                var numbering = document.numbering.Descendants().Where(d => d.Name.LocalName == "num");
                Assert.IsFalse(numbering.Any());

                document.AddList("List Text");

                numbering = document.numbering.Descendants().Where(d => d.Name.LocalName == "num");
                Assert.IsTrue(numbering.Any());
            }
        }

        [Test]
        public void CreateNewNumberingNumIdShouldAddNumberingDataToTheDocument()
        {
            using (DocX document = DocX.Create("TestCreateNumbering.docx"))
            {
                var numbering = document.numbering.Descendants().Where(d => d.Name.LocalName == "num");
                Assert.IsFalse(numbering.Any());
                var list = document.AddList("", 0, ListItemType.Bulleted);
                document.InsertList(list);

                numbering = document.numbering.Descendants().Where(d => d.Name.LocalName == "num");
                Assert.IsTrue(numbering.Any());
            }
        }

        [Test]
        public void CreateNewNumberingNumIdShouldAddNumberingAbstractDataToTheDocument()
        {
            using (DocX document = DocX.Create("TestCreateNumberingAbstract.docx"))
            {
                var numbering = document.numbering.Descendants().Where(d => d.Name.LocalName == "abstractNum");
                Assert.IsFalse(numbering.Any());
                var list = document.AddList("", 0, ListItemType.Bulleted);
                document.InsertList(list);

                numbering = document.numbering.Descendants().Where(d => d.Name.LocalName == "abstractNum");
                Assert.IsTrue(numbering.Any());
            }
        }

        [Test]
        public void IfPreviousElementIsAListThenAddingANewListContinuesThePreviousList()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "TestAddListToPreviousList.docx")))
            {
                var list = document.AddList("List Text");
                document.AddListItem(list, "List Text2");
                document.InsertList(list);

                var lvlNodes = document.mainDoc.Descendants().Where(s => s.Name.LocalName == "ilvl").ToList();
                var numIdNodes = document.mainDoc.Descendants().Where(s => s.Name.LocalName == "numId").ToList();

                Assert.AreEqual(lvlNodes.Count(), 2);
                Assert.AreEqual(numIdNodes.Count(), 2);

                var prevLvlNode = lvlNodes[0];
                var newLvlNode = lvlNodes[1];

                Assert.AreEqual(prevLvlNode.Attribute(DocX.w + "val").Value, newLvlNode.Attribute(DocX.w + "val").Value);

                var prevNumIdNode = numIdNodes[0];
                var newNumIdNode = numIdNodes[1];

                Assert.AreEqual(prevNumIdNode.Attribute(DocX.w + "val").Value, newNumIdNode.Attribute(DocX.w + "val").Value);
                document.Save();
            }

        }

        [Test]
        public void WhenADocumentHasListsTheListPropertyReturnsTheCorrectNumberOfLists()
        {

            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "testdoc_OrderedUnorderedLists.docx")))
            {
                var lists = document.Lists;

                Assert.AreEqual(lists.Count, 2);

            }
        }

        [Test]
        public void WhenADocumentIsCreatedWithAListItemThatHasASpecifiedStartNumber()
        {
            using (DocX document = DocX.Create("CreateListItemFromDifferentStartValue.docx"))
            {
                var list = document.AddList("Test", 0, ListItemType.Numbered, 5);
                document.AddListItem(list, "NewElement");

                var numbering = document.numbering.Descendants().Where(d => d.Name.LocalName == "abstractNum");
                var level = numbering.Descendants().First(el => el.Name.LocalName == "lvl");
                var start = level.Descendants().First(el => el.Name.LocalName == "start");
                Assert.AreEqual(start.GetAttribute(DocX.w + "val"), 5.ToString());
            }
        }

        [Test]
        public void WhenANumberedAndBulletedListIsCreatedThereShouldBeTwoNumberingXmls()
        {
            using (DocX document = DocX.Create("NumberAndBulletListInOne.docx"))
            {
                var numberList = document.AddList("Test");
                document.AddListItem(numberList, "Second Numbered Item");

                var bulletedList = document.AddList("Bullet", 0, ListItemType.Bulleted);
                document.AddListItem(bulletedList, "Second bullet item");

                document.InsertList(numberList);
                document.InsertList(bulletedList);

                var abstractNums = document.numbering.Descendants().Where(d => d.Name.LocalName == "abstractNum");
                Assert.AreEqual(abstractNums.Count(), 2);

            }
        }

        [Test]
        public void WhenICreateAnEmptyListAndAddEntriesToIt()
        {
            using (DocX document = DocX.Create("CreateEmptyListAndAddItemsToIt.docx"))
            {
                var list = document.AddList();
                Assert.AreEqual(list.Items.Count, 0);

                document.AddListItem(list, "Test item 1.");
                document.AddListItem(list, "Test item 2.");
                Assert.AreEqual(list.Items.Count, 2);
            }
        }

        [Test]
        public void WhenICreateAHeaderItShouldHaveAStyle()
        {
            using (var document = DocX.Create(Path.Combine(_directoryDocuments, "CreateHeaderElement.docx")))
            {
                document.InsertParagraph("Header Text 1").StyleName = "Header1";
                Assert.IsNotNull(document.styles.Root.Descendants().FirstOrDefault(d => d.GetAttribute(DocX.w + "styleId").ToLowerInvariant() == "heading1"));
                document.Save();
            }
        }

        [Test]
        public void ParagraphAppendHyperLink_ParagraphIsListItem_ShouldNotThrow()
        {
            using (var document = DocX.Create(Path.Combine(_directoryDocuments, "HyperlinkList.docx")))
            {
                var list = document.AddList("Item 1");
                document.AddListItem(list, "Item 2");
                document.AddListItem(list, "Item 3");

                Uri uri;
                Uri.TryCreate("http://www.google.com", UriKind.RelativeOrAbsolute, out uri);
                var hLink = document.AddHyperlink("Google", uri);
                var item2 = list.Items[1];

                item2.InsertText("\nMore text\n");
                item2.AppendHyperlink(hLink);

                item2.InsertText("\nEven more text");

                document.InsertList(list);
                document.Save();
            }
        }


        [Test]
        public void WhileReadingWhenTextIsBoldItalicUnderlineItShouldReadTheProperFormatting()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "FontFormat.docx")))
            {
                var underlinedTextFormatting = document.Paragraphs[0].MagicText[0].formatting;
                var boldTextFormatting = document.Paragraphs[0].MagicText[2].formatting;
                var italicTextFormatting = document.Paragraphs[0].MagicText[4].formatting;
                var boldItalicUnderlineTextFormatting = document.Paragraphs[0].MagicText[6].formatting;

                Assert.IsTrue(boldTextFormatting.Bold.HasValue && boldTextFormatting.Bold.Value);
                Assert.IsTrue(italicTextFormatting.Italic.HasValue && italicTextFormatting.Italic.Value);
                Assert.AreEqual(underlinedTextFormatting.UnderlineStyle, UnderlineStyle.singleLine);
                Assert.IsTrue(boldItalicUnderlineTextFormatting.Bold.HasValue && boldItalicUnderlineTextFormatting.Bold.Value);
                Assert.IsTrue(boldItalicUnderlineTextFormatting.Italic.HasValue && boldItalicUnderlineTextFormatting.Italic.Value);
                Assert.AreEqual(boldItalicUnderlineTextFormatting.UnderlineStyle, UnderlineStyle.singleLine);
            }
        }


        [Test]
        public void WhileWritingWhenTextIsBoldItalicUnderlineItShouldReadTheProperFormatting()
        {
            using (DocX document = DocX.Create("FontFormatWrite.docx"))
            {

                Paragraph p = document.InsertParagraph();
                p.Append("This is bold.").Bold().Append("This is underlined.").UnderlineStyle(UnderlineStyle.singleLine).
                Append("This is italic.").Italic().Append("This is boldItalicUnderlined").Italic().Bold().UnderlineStyle(UnderlineStyle.singleLine);

                var boldTextFormatting = document.Paragraphs[0].MagicText[0].formatting;
                var underlinedTextFormatting = document.Paragraphs[0].MagicText[1].formatting;
                var italicTextFormatting = document.Paragraphs[0].MagicText[2].formatting;
                var boldItalicUnderlineTextFormatting = document.Paragraphs[0].MagicText[3].formatting;

                Assert.IsTrue(boldTextFormatting.Bold.HasValue && boldTextFormatting.Bold.Value);
                Assert.IsTrue(italicTextFormatting.Italic.HasValue && italicTextFormatting.Italic.Value);
                Assert.AreEqual(underlinedTextFormatting.UnderlineStyle, UnderlineStyle.singleLine);
                Assert.IsTrue(boldItalicUnderlineTextFormatting.Bold.HasValue && boldItalicUnderlineTextFormatting.Bold.Value);
                Assert.IsTrue(boldItalicUnderlineTextFormatting.Italic.HasValue && boldItalicUnderlineTextFormatting.Italic.Value);
                Assert.AreEqual(boldItalicUnderlineTextFormatting.UnderlineStyle, UnderlineStyle.singleLine);
            }
        }

        [Test]
        public void InsertingANextPageBreakShouldAddADocumentSection()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "SectionPageBreak.docx")))
            {
                document.InsertSectionPageBreak();

                var sections = document.GetSections();
                Assert.AreEqual(sections.Count, 2);
                document.Save();
            }
        }

        [Test]
        public void InsertANextPageBreakWithParagraphTextsShouldAddProperParagraphsToProperSections()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "SectionPageBreakWithParagraphs.docx")))
            {
                document.InsertParagraph("First paragraph.");
                document.InsertParagraph("Second paragraph.");
                document.InsertSectionPageBreak();
                document.InsertParagraph("Third paragraph.");
                document.InsertParagraph("Fourth paragraph.");

                var sections = document.GetSections();
                Assert.AreEqual(sections.Count, 2);

                Assert.AreEqual(sections[0].SectionParagraphs.Count(p => !string.IsNullOrWhiteSpace(p.Text)), 2);
                Assert.AreEqual(sections[1].SectionParagraphs.Count(p => !string.IsNullOrWhiteSpace(p.Text)), 2);
                document.Save();
            }
        }

        [Test]
        public void WhenAFontFamilyIsSpecifiedForAParagraphItShouldSetTheFontOfTheParagraphTextToTheFontFamily()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, "FontTest.docx")))
            {
                Paragraph p = document.InsertParagraph();

                var fontFamily = new Font("Symbol");

                p.Append("Hello World").Font(fontFamily);

                Assert.AreEqual(p.MagicText[0].formatting.FontFamily.Name, fontFamily.Name);

                document.Save();
            }
        }

        [Test]
        public void Test_Paragraph_RemoveTextManyLetters()
        {
            using (DocX document = DocX.Create(@"HelloWorldRemovingManyLetters.docx"))
            {

                Paragraph p3 = document.InsertParagraph("");
                p3.Xml = XElement.Parse(
                    @"<w:p xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main"">
                    <w:pPr>
                    <w:ind />
                    </w:pPr>
                    <w:r>
                    <w:t>Based on the previous screening criteria, you qualify to participate in this particular survey. At the completion of the survey, you will be notified that your responses have been received and honoraria information will be captured for future payment. Thank you in advance for taking the time to participate with us. ^f('xMinutes').get() == 'xx' ? """" : ""</w:t>
                    </w:r>
                    <w:r>
                    <w:rPr>
                        <w:lang w:val=""pl-PL"" />
                    </w:rPr>
                    <w:t xml:space=""preserve"">This survey should take </w:t>
                    </w:r>
                    <w:r>
                    <w:t>"" + f('xMinutes').get() + ""</w:t>
                    </w:r>
                    <w:r>
                    <w:rPr>
                        <w:lang w:val=""pl-PL"" />
                    </w:rPr>
                    <w:t xml:space=""preserve""> minutes.  </w:t>
                    </w:r>
                    <w:r>
                    <w:t>""^Participants completing this survey will receive the honorarium designated in the invitation you have received. &lt;BR&gt;&lt;BR&gt;If you leave the survey prior to finishing it, you may return to your last question by visiting the same link provided in your email invitation (please be certain to use the same email that you used a moment ago to register for this study). If you have any questions or concerns about this study, please contact us at &lt;a href=""mailto:blabla@blabla.com?Subject=^f('sName')^ PD:^f('pdID')^""&gt;blabla@blabla.com&lt;/a&gt;. Thank you.</w:t>
                    </w:r>
                    </w:p>");

                int l1 = p3.Text.Length; //960
                p3.RemoveText(318, 99);
                int l2 = p3.Text.Length; //should be 861
                Assert.AreEqual(l1 - 99, l2);
            }
        }

        [Test]
        public void Test_Table_RemoveParagraphs()
        {
            var memoryStream = new MemoryStream();
            var document = DocX.Create(memoryStream);
            // Add a Table into the document.
            Table table = document.AddTable(1, 4); // 1 row, 4 cells
            table.Design = TableDesign.TableGrid;
            table.Alignment = Alignment.center;
            // Edit row
            var row = table.Rows[0];

            // Fill 1st paragraph
            row.Cells[0].Paragraphs.ElementAt(0).Append("Paragraph 1");
            // Fill 2nd paragraph
            var secondParagraph = row.Cells[0].InsertParagraph("Paragraph 2");

            // Check number of paragraphs
            Assert.AreEqual(2, row.Cells[0].Paragraphs.Count());

            // Remove 1st paragraph
            var deleted = row.Cells[0].RemoveParagraphAt(0);
            Assert.IsTrue(deleted);
            // Check number of paragraphs
            Assert.AreEqual(1, row.Cells[0].Paragraphs.Count());

            // Remove 3rd (nonexisting) paragraph
            deleted = row.Cells[0].RemoveParagraphAt(3);
            Assert.IsFalse(deleted);
            //check number of paragraphs
            Assert.AreEqual(1, row.Cells[0].Paragraphs.Count());

            // Remove secondParagraph (this time the only one) paragraph
            deleted = row.Cells[0].RemoveParagraph(secondParagraph);
            Assert.IsTrue(deleted);
            Assert.AreEqual(0, row.Cells[0].Paragraphs.Count());

            // Remove last paragraph once again - this time this paragraph does not exists
            deleted = row.Cells[0].RemoveParagraph(secondParagraph);
            Assert.IsFalse(deleted);
            Assert.AreEqual(0, row.Cells[0].Paragraphs.Count());
        }
        [Test]
        public void GenerateHeadingTestDocument()
        {
            using (DocX document = DocX.Create(Path.Combine(_directoryDocuments, @"Document Header Test.docx")))
            {

                foreach (HeadingType heading in (HeadingType[])Enum.GetValues(typeof(HeadingType)))
                {
                    string text = string.Format("{0} - The quick brown fox jumps over the lazy dog", heading.EnumDescription());

                    Paragraph p = document.InsertParagraph();
                    p.AppendLine(text).Heading(heading);
                }


                document.Save();
            }
        }



        [Test]
        public void CreateTableOfContents_WithTitleAndSwitches_SetsExpectedXml()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const int rightPos = 9350;
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                var toc = TableOfContents.CreateTableOfContents(document, title, switches);

                const string switchString = @"TOC \h \o '1-3' \u \z";
                var expectedString = string.Format(XmlTemplateBases.TocXmlBase, "TOCHeading", title, rightPos, switchString);
                var expectedReader = XmlReader.Create(new StringReader(expectedString));
                var expected = XElement.Load(expectedReader);

                Assert.IsTrue(XNode.DeepEquals(expected, toc.Xml));
            }
        }

        [Test]
        public void CreateTableOfContents_WithTitleSwitchesHeaderStyleLastIncludeLevelRightTabPos_SetsExpectedXml()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const int rightPos = 1337;
                const string style = "TestStyle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                var toc = TableOfContents.CreateTableOfContents(document, title, switches, style, 6, rightPos);

                const string switchString = @"TOC \h \o '1-6' \u \z";
                var expectedString = string.Format(XmlTemplateBases.TocXmlBase, style, title, rightPos, switchString);

                var expectedReader = XmlReader.Create(new StringReader(expectedString));
                var expected = XElement.Load(expectedReader);

                Assert.IsTrue(XNode.DeepEquals(expected, toc.Xml));
            }
        }

        [Test]
        public void CreateTableOfContents_WhenCalled_AddsUpdateFieldsWithValueTrueToSettings()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                TableOfContents.CreateTableOfContents(document, title, switches);

                var updateField = document.settings.Descendants().FirstOrDefault(x => x.Name == DocX.w + "updateFields");
                Assert.IsNotNull(updateField);
                Assert.AreEqual("true", updateField.Attribute(DocX.w + "val").Value);
            }
        }

        [Test]
        public void CreateTableOfContents_WhenCalledSettingsAlreadyHasUpdateFields_DoesNotAddUpdateFields()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                var element = new XElement(XName.Get("updateFields", DocX.w.NamespaceName), new XAttribute(DocX.w + "val", true));
                document.settings.Root.Add(element);
                const string title = "TestTitle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                TableOfContents.CreateTableOfContents(document, title, switches);

                var updateFields = document.settings.Descendants().Single(x => x.Name == DocX.w + "updateFields");
                Assert.AreSame(element, updateFields);
            }
        }

        [Test]
        public void CreteTableOfContents_TocHeadingStyleIsNotPresent_AddsTocHeaderStyle()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const string headerStyle = "TestStyle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                TableOfContents.CreateTableOfContents(document, title, switches, headerStyle);

                var expectedString = string.Format(XmlTemplateBases.TocHeadingStyleBase, headerStyle);

                var expectedReader = XmlReader.Create(new StringReader(expectedString));
                var expected = XElement.Load(expectedReader);

                var actual = document.styles.Root.Descendants().FirstOrDefault(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals(headerStyle));

                Assert.IsTrue(XNode.DeepEquals(expected, actual));
            }
        }

        [Test]
        public void CreteTableOfContents_Toc1StyleIsNotPresent_AddsToc1Style()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                TableOfContents.CreateTableOfContents(document, title, switches);

                var expectedString = string.Format(XmlTemplateBases.TocElementStyleBase, "TOC1", "toc 1");

                var expectedReader = XmlReader.Create(new StringReader(expectedString));
                var expected = XElement.Load(expectedReader);

                var actual = document.styles.Root.Descendants().FirstOrDefault(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("TOC1"));

                Assert.IsTrue(XNode.DeepEquals(expected, actual));
            }
        }

        [Test]
        public void CreteTableOfContents_Toc2StyleIsNotPresent_AddsToc2Style()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                TableOfContents.CreateTableOfContents(document, title, switches);

                var expectedString = string.Format(XmlTemplateBases.TocElementStyleBase, "TOC2", "toc 2");

                var expectedReader = XmlReader.Create(new StringReader(expectedString));
                var expected = XElement.Load(expectedReader);

                var actual = document.styles.Root.Descendants().FirstOrDefault(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("TOC2"));

                Assert.IsTrue(XNode.DeepEquals(expected, actual));
            }
        }

        [Test]
        public void CreteTableOfContents_Toc3StyleIsNotPresent_AddsToc3tyle()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                TableOfContents.CreateTableOfContents(document, title, switches);

                var expectedString = string.Format(XmlTemplateBases.TocElementStyleBase, "TOC3", "toc 3");

                var expectedReader = XmlReader.Create(new StringReader(expectedString));
                var expected = XElement.Load(expectedReader);

                var actual = document.styles.Root.Descendants().FirstOrDefault(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("TOC3"));

                Assert.IsTrue(XNode.DeepEquals(expected, actual));
            }
        }

        [Test]
        public void CreteTableOfContents_Toc4StyleIsNotPresent_AddsToc4Style()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                TableOfContents.CreateTableOfContents(document, title, switches);

                var expectedString = string.Format(XmlTemplateBases.TocElementStyleBase, "TOC4", "toc 4");

                var expectedReader = XmlReader.Create(new StringReader(expectedString));
                var expected = XElement.Load(expectedReader);

                var actual = document.styles.Root.Descendants().FirstOrDefault(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("TOC4"));

                Assert.IsTrue(XNode.DeepEquals(expected, actual));
            }
        }

        [Test]
        public void CreteTableOfContents_HyperlinkStyleIsNotPresent_AddsHyperlinkStyle()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                TableOfContents.CreateTableOfContents(document, title, switches);

                var expectedString = XmlTemplateBases.TocHyperLinkStyleBase;

                var expectedReader = XmlReader.Create(new StringReader(expectedString));
                var expected = XElement.Load(expectedReader);

                var actual = document.styles.Root.Descendants().FirstOrDefault(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("character") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("Hyperlink"));

                Assert.IsTrue(XNode.DeepEquals(expected, actual));
            }
        }

        [Test]
        public void CreteTableOfContents_TocHeadingStyleIsPresent_DoesNotAddTocHeaderStyle()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const string headerStyle = "TestStyle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                var xElement = XElement.Load(XmlReader.Create(new StringReader(string.Format(XmlTemplateBases.TocHeadingStyleBase, headerStyle))));
                document.styles.Root.Add(xElement);

                TableOfContents.CreateTableOfContents(document, title, switches, headerStyle);

                var actual = document.styles.Root.Descendants().Single(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals(headerStyle));

                Assert.AreSame(xElement, actual);
            }
        }

        [Test]
        public void CreteTableOfContents_Toc1StyleIsPresent_DoesNotAddToc1Style()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const string headerStyle = "TestStyle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                var xElement = XElement.Load(XmlReader.Create(new StringReader(string.Format(XmlTemplateBases.TocElementStyleBase, "TOC1", "toc 1"))));
                document.styles.Root.Add(xElement);

                TableOfContents.CreateTableOfContents(document, title, switches, headerStyle);

                var actual = document.styles.Root.Descendants().Single(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("TOC1"));

                Assert.AreSame(xElement, actual);
            }
        }

        [Test]
        public void CreteTableOfContents_Toc2StyleIsPresent_DoesNotAddToc2Style()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const string headerStyle = "TestStyle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                var xElement = XElement.Load(XmlReader.Create(new StringReader(string.Format(XmlTemplateBases.TocElementStyleBase, "TOC2", "toc 2"))));
                document.styles.Root.Add(xElement);

                TableOfContents.CreateTableOfContents(document, title, switches, headerStyle);

                var actual = document.styles.Root.Descendants().Single(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("TOC2"));

                Assert.AreSame(xElement, actual);
            }
        }

        [Test]
        public void CreteTableOfContents_Toc3StyleIsPresent_DoesNotAddToc3Style()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const string headerStyle = "TestStyle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                var xElement = XElement.Load(XmlReader.Create(new StringReader(string.Format(XmlTemplateBases.TocElementStyleBase, "TOC3", "toc 3"))));
                document.styles.Root.Add(xElement);

                TableOfContents.CreateTableOfContents(document, title, switches, headerStyle);

                var actual = document.styles.Root.Descendants().Single(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("TOC3"));

                Assert.AreSame(xElement, actual);
            }
        }

        [Test]
        public void CreteTableOfContents_Toc4StyleIsPresent_DoesNotAddToc4Style()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const string headerStyle = "TestStyle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                var xElement = XElement.Load(XmlReader.Create(new StringReader(string.Format(XmlTemplateBases.TocElementStyleBase, "TOC4", "toc 4"))));
                document.styles.Root.Add(xElement);

                TableOfContents.CreateTableOfContents(document, title, switches, headerStyle);

                var actual = document.styles.Root.Descendants().Single(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("paragraph") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("TOC4"));

                Assert.AreSame(xElement, actual);
            }
        }

        [Test]
        public void CreteTableOfContents_HyperlinkStyleIsPresent_DoesNotAddHyperlinkStyle()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string title = "TestTitle";
                const TableOfContentsSwitches switches =
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U;

                var xElement = XElement.Load(XmlReader.Create(new StringReader(XmlTemplateBases.TocHyperLinkStyleBase)));
                document.styles.Root.Add(xElement);

                TableOfContents.CreateTableOfContents(document, title, switches);

                var actual = document.styles.Root.Descendants().Single(x =>
                                x.Name.Equals(DocX.w + "style") &&
                                x.Attribute(DocX.w + "type").Value.Equals("character") &&
                                x.Attribute(DocX.w + "styleId").Value.Equals("Hyperlink"));

                Assert.AreSame(xElement, actual);
            }
        }

        [Test]
        public void InsertDefaultTableOfContents_WhenCalled_AddsTocToDocument()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                document.InsertDefaultTableOfContents();

                var toc = TableOfContents.CreateTableOfContents(document, "Table of contents",
                    TableOfContentsSwitches.O | TableOfContentsSwitches.H | TableOfContentsSwitches.Z |
                    TableOfContentsSwitches.U);

                Assert.IsTrue(document.Xml.Descendants().FirstOrDefault(x => XNode.DeepEquals(toc.Xml, x)) != null);
            }
        }

        [Test]
        public void InsertTableOfContents_WhenCalledWithTitleSwitchesHeaderStyleMaxIncludeLevelAndRightTabPos_AddsTocToDocument()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string tableOfContentsTitle = "Table of contents";
                const TableOfContentsSwitches tableOfContentsSwitches = TableOfContentsSwitches.O | TableOfContentsSwitches.A;
                const string headerStyle = "HeaderStyle";
                const int lastIncludeLevel = 4;
                const int rightTabPos = 1337;

                document.InsertTableOfContents(tableOfContentsTitle, tableOfContentsSwitches, headerStyle, lastIncludeLevel, rightTabPos);

                var toc = TableOfContents.CreateTableOfContents(document, tableOfContentsTitle, tableOfContentsSwitches, headerStyle, lastIncludeLevel, rightTabPos);

                Assert.IsTrue(document.Xml.Descendants().FirstOrDefault(x => XNode.DeepEquals(toc.Xml, x)) != null);
            }
        }

        [Test]
        public void InsertTableOfContents_WhenCalledWithReferenceTitleSwitchesHeaderStyleMaxIncludeLevelAndRightTabPos_AddsTocToDocumentAtExpectedLocation()
        {
            using (var document = DocX.Create("TableOfContents Test.docx"))
            {
                const string tableOfContentsTitle = "Table of contents";
                const TableOfContentsSwitches tableOfContentsSwitches = TableOfContentsSwitches.O | TableOfContentsSwitches.A;
                const string headerStyle = "HeaderStyle";
                const int lastIncludeLevel = 4;
                const int rightTabPos = 1337;

                document.InsertParagraph("Paragraph1");
                var p2 = document.InsertParagraph("Paragraph2");
                var p3 = document.InsertParagraph("Paragraph3");

                document.InsertTableOfContents(p3, tableOfContentsTitle, tableOfContentsSwitches, headerStyle, lastIncludeLevel, rightTabPos);

                var toc = TableOfContents.CreateTableOfContents(document, tableOfContentsTitle, tableOfContentsSwitches, headerStyle, lastIncludeLevel, rightTabPos);

                var tocElement = document.Xml.Descendants().FirstOrDefault(x => XNode.DeepEquals(toc.Xml, x));

                Assert.IsTrue(p2.Xml.IsBefore(tocElement));
                Assert.IsTrue(tocElement.IsAfter(p2.Xml));
                Assert.IsTrue(tocElement.IsBefore(p3.Xml));
                Assert.IsTrue(p3.Xml.IsAfter(tocElement));
            }
        }

        [Test]
        public void ValidateBookmark_WhenCalledWithNameOfNonMatchingBookmark_ReturnsFalse()
        {
            using (var document = DocX.Create("Bookmark validate.docx"))
            {
                var p = document.InsertParagraph("No bookmark here");

                Assert.IsFalse(p.ValidateBookmark("Team Rubberduck"));
            }
        }

        [Test]
        public void ValidateBookmark_WhenCalledWithNameOfMatchingBookmark_ReturnsTrue()
        {
            using (var document = DocX.Create("Bookmark validate.docx"))
            {
                var p = document.InsertParagraph("Here's a bookmark!");
                const string bookmarkName = "Team Rubberduck";

                p.AppendBookmark(bookmarkName);

                Assert.IsTrue(p.ValidateBookmark("Team Rubberduck"));
            }
        }

        [Test]
        public void ValidateBookmarks_WhenCalledWithMatchingBookmarkNameInHeader_ReturnsEmpty()
        {
            using (var document = DocX.Create("Bookmark validate.docx"))
            {
                document.AddHeaders();
                var p = document.Headers.first.InsertParagraph("Here's a bookmark!");
                const string bookmarkName = "Team Rubberduck";

                p.AppendBookmark(bookmarkName);

                Assert.IsTrue(document.ValidateBookmarks("Team Rubberduck").Length == 0);
            }
        }

        [Test]
        public void ValidateBookmarks_WhenCalledWithMatchingBookmarkNameInMainDocument_ReturnsEmpty()
        {
            using (var document = DocX.Create("Bookmark validate.docx"))
            {
                var p = document.InsertParagraph("Here's a bookmark!");
                const string bookmarkName = "Team Rubberduck";

                p.AppendBookmark(bookmarkName);

                Assert.IsTrue(document.ValidateBookmarks("Team Rubberduck").Length == 0);
            }
        }

        [Test]
        public void ValidateBookmarks_WhenCalledWithMatchingBookmarkNameInFooters_ReturnsEmpty()
        {
            using (var document = DocX.Create("Bookmark validate.docx"))
            {
                document.AddFooters();
                var p = document.Footers.first.InsertParagraph("Here's a bookmark!");
                const string bookmarkName = "Team Rubberduck";

                p.AppendBookmark(bookmarkName);

                Assert.IsTrue(document.ValidateBookmarks("Team Rubberduck").Length == 0);
            }
        }

        [Test]
        public void ValidateBookmarks_WhenCalledWithNoMatchingBookmarkNames_ReturnsExpected()
        {
            using (var document = DocX.Create("Bookmark validate.docx"))
            {
                document.AddHeaders();
                var p = document.Headers.first.InsertParagraph("Here's a bookmark!");

                p.AppendBookmark("Not in search");

                var bookmarkNames = new[] { "Team Rubberduck", "is", "the most", "awesome people" };

                var result = document.ValidateBookmarks(bookmarkNames);
                for (var i = 0; i < bookmarkNames.Length; i++)
                {
                    Assert.AreEqual(bookmarkNames[i], result[i]);
                }
            }
        }

        [Test]
        public void CreateTable_WhenCalledSetColumnWidth_ReturnsExpected()
        {
            using (var document = DocX.Create("Set column width.docx"))
            {
                var table = document.InsertTable(1, 2);

                table.SetColumnWidth(0, 1000);
                table.SetColumnWidth(1, 2000);

                Assert.AreEqual(1000, table.GetColumnWidth(0));
                Assert.AreEqual(2000, table.GetColumnWidth(1));
            }
        }

        [Test]
        public void UpdateParagraphFontSize_WhenSetFontSize_ReturnsExpected()
        {
            using (var document = DocX.Create("Update paragraph font size.docx"))
            {
                var paragraph = document.InsertParagraph().FontSize(9);

                paragraph.FontSize(11);

                string szValue = paragraph.Xml.Descendants(XName.Get("sz", DocX.w.NamespaceName))
                   .Attributes(XName.Get("val", DocX.w.NamespaceName)).First().Value;
                string szCsValue = paragraph.Xml.Descendants(XName.Get("szCs", DocX.w.NamespaceName))
                   .Attributes(XName.Get("val", DocX.w.NamespaceName)).First().Value;

                // the expected value is multiplied by 2
                // and the last font size is 11*2 = 22
                Assert.AreEqual("22", szValue);
                Assert.AreEqual("22", szCsValue);
            }
        }

        [Test]
        public void SetTableCellMargin_WhenSetTwoCellMargins_ContainsTwoCellMargins()
        {
            using (var document = DocX.Create("Set table cell margins.docx"))
            {
                Table table = document.InsertTable(1, 1);

                table.SetTableCellMargin(TableCellMarginType.left, 20);
                table.SetTableCellMargin(TableCellMarginType.right, 40);

                var elements = table.Xml.Descendants(XName.Get("tblCellMar", DocX.w.NamespaceName)).ToList();
                // should contain only one element named tblCellMar
                Assert.AreEqual(1, elements.Count);
                var element = elements.First();
                // should contain two elements defining the margins
                Assert.AreEqual(2, element.Elements().Count());

                var left = element.Element(XName.Get("left", DocX.w.NamespaceName));
                var right = element.Element(XName.Get("right", DocX.w.NamespaceName));

                Assert.IsNotNull(left);
                Assert.IsNotNull(right);

                Assert.AreEqual("20", left.Attribute(XName.Get("w", DocX.w.NamespaceName)).Value);
                Assert.AreEqual("dxa", left.Attribute(XName.Get("type", DocX.w.NamespaceName)).Value);

                Assert.AreEqual("40", right.Attribute(XName.Get("w", DocX.w.NamespaceName)).Value);
                Assert.AreEqual("dxa", right.Attribute(XName.Get("type", DocX.w.NamespaceName)).Value);
            }
        }

        [Test]
        public void SetTableCellMargin_WhenReSetCellMargin_ContainsOneCellMargin()
        {
            using (var document = DocX.Create("Set table cell margin.docx"))
            {
                Table table = document.InsertTable(1, 1);

                table.SetTableCellMargin(TableCellMarginType.left, 20);
                // change the table cell margin
                table.SetTableCellMargin(TableCellMarginType.left, 40);

                var elements = table.Xml.Descendants(XName.Get("tblCellMar", DocX.w.NamespaceName)).ToList();
                // should contain only one element named tblCellMar
                Assert.AreEqual(1, elements.Count);
                var element = elements.First();
                // should contain two elements defining the margins
                Assert.AreEqual(1, element.Elements().Count());

                var left = element.Element(XName.Get("left", DocX.w.NamespaceName));

                Assert.IsNotNull(left);

                // check that the last value is taken
                Assert.AreEqual("40", left.Attribute(XName.Get("w", DocX.w.NamespaceName)).Value);
                Assert.AreEqual("dxa", left.Attribute(XName.Get("type", DocX.w.NamespaceName)).Value);
            }
        }

        [Test]
        public void WhileReadingWhenTextIsSuperAndSubscript()
        {
            using (DocX document = DocX.Load(Path.Combine(_directoryWithFiles, "Super_Subscript.docx")))
            {
                var normalText = document.Paragraphs[0].MagicText[0].formatting;
                var superscriptText = document.Paragraphs[0].MagicText[3].formatting;
                var subscriptText = document.Paragraphs[0].MagicText[5].formatting;

                Assert.IsTrue(normalText.Script.HasValue && normalText.Script == Script.none);
                Assert.IsTrue(superscriptText.Script.HasValue && superscriptText.Script == Script.superscript);
                Assert.IsTrue(subscriptText.Script.HasValue && subscriptText.Script == Script.subscript);
            }
        }
    }
}

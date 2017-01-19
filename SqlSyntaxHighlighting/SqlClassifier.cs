using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using SqlSyntaxHighlighting.NaturalTextTaggers;

namespace SqlSyntaxHighlighting
{
	class SqlClassifier : IClassifier
	{
		private readonly char[] keywordPrefixCharacters = new[] { '\t', ' ', '"', '(' };
		private readonly char[] keywordPostfixCharacters = new[] { '\t', ' ', '"', '(', ')', ';' };
		private readonly char[] functionPrefixCharacters = new[] { '\t', ' ', '"', ',', '(' };
		private readonly char[] functionPostfixCharacters = new[] { '\t', '(' };

		private readonly List<string> keywords = new List<string> {
			"SELECT", "INSERT", "DELETE", "UPDATE",
			"ONLY",
			"INTO", "VALUES", "TRUNCATE", "DISTINCT", "TOP", "WITH",
			"FROM", "JOIN", "INNER JOIN", "OUTER JOIN", "LEFT OUTER JOIN", "RIGHT OUTER JOIN", "LEFT JOIN", "RIGHT JOIN", "CROSS JOIN",
			"UNION", "EXCEPT",
			"WHERE", "LIKE", "BETWEEN", "HAVING", "EXISTS",
			"ORDER BY", "ASC", "DESC", "OVER", "GROUP BY", "LIMIT", "OFFSET",
			"PARTITION BY", "WINDOW",
			"ON", "IN", "IS", "NOT", "AS", "AND", "OR", "ALL", "ANY",
			"CREATE", "ALTER", "DROP",
			"TEMP", "TEMPORARY",
			"TABLE", "FUNCTION", "PROCEDURE", "VIEW", "SCHEMA",
			"DECLARE", "SET", "READ ONLY",
			"IF", "BEGIN", "THEN", "ELSE", "END", "FOR", "WHILE", "NULL", "CASE", "WHEN",
			"TRANSACTION", "COMMIT", "ROLLBACK",
			"EXEC", "RETURN", "RETURNS", "PRINT", "USE", "USING", "RETURNING",
			"COPY", "STDIN", "STDOUT",

			"BIGINT", "NUMERIC", "BIT", "SMALLINT", "DECIMAL", "SMALLMONEY", "INT", "TINYINT", "MONEY", "FLOAT", "REAL",
			"DATE", "DATETIMEOFFSET", "DATETIME2", "SMALLDATETIME", "DATETIME", "TIME", "TIMESTAMP",
			"CHAR", "VARCHAR", "TEXT", "NCHAR", "NVARCHAR", "NTEXT",
			"BINARY", "VARBINARY", "IMAGE",
			"CURSOR", "HIERARCHYID", "UNIQUEIDENTIFIER", "SQL_VARIANT", "XML"
		};

		private readonly List<string> functions = new List<string> {
			"COUNT", "COUNT_BIG", "SUM", "MIN", "MAX", "AVG",
			"ABS", "NEWID", "RAND", "ISNULL", "COALESCE",
			"LEFT", "RIGHT", "SUBSTRING", "LTRIM", "RTRIM", "UPPER", "LOWER", "CHARINDEX", "LEN", "STUFF",
			"GETDATE", "DATEADD", "DATEDIFF", "DATEPART", "DATENAME",
			"CONVERT", "CAST",
			"ROW_NUMBER", "NULLIF"
		};

		private readonly Regex variables = new Regex(@"(?:^|[""\s(+,=])(?<Variable>[@\:][a-zA-Z0-9_]+)(?:$|[""\s)+,])", RegexOptions.Multiline);


		private readonly IClassificationType keywordType;
		private readonly IClassificationType functionType;
		private readonly IClassificationType variableType;
		readonly ITagAggregator<NaturalTextTag> tagger;

		internal SqlClassifier(ITagAggregator<NaturalTextTag> tagger, IClassificationTypeRegistryService classificationRegistry)
		{
			this.tagger = tagger;
			keywordType = classificationRegistry.GetClassificationType("sql-keyword");
			functionType = classificationRegistry.GetClassificationType("sql-function");
			variableType = classificationRegistry.GetClassificationType("sql-variable");
		}

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			IList<ClassificationSpan> classifiedSpans = new List<ClassificationSpan>();

			var tags = tagger.GetTags(span).ToList();
			foreach (IMappingTagSpan<NaturalTextTag> tagSpan in tags)
			{
				SnapshotSpan snapshot = tagSpan.Span.GetSpans(span.Snapshot).First();

				string text = snapshot.GetText();
				int index = -1;

				// keywords
				foreach (string keyword in keywords)
				{
					while (snapshot.Length > index + 1 && (index = text.IndexOf(keyword, index + 1)) > -1)
					{
						// controleren of het gevonden keyword niet tegen of in een ander woord staat
						if ((index > 0 && keywordPrefixCharacters.Contains(text[index - 1]) == false) ||
							(index + keyword.Length < text.Length && keywordPostfixCharacters.Contains(text[index + keyword.Length]) == false))
							continue;

						classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, keyword.Length), keywordType));
					}
				}

				// functions
				foreach (string function in functions)
				{
					while (snapshot.Length > index + 1 && (index = text.IndexOf(function, index + 1)) > -1)
					{
						// controleren of het gevonden keyword niet tegen of in een ander woord staat
						if ((index > 0 && functionPrefixCharacters.Contains(text[index - 1]) == false) ||
							(index + function.Length < text.Length && functionPostfixCharacters.Contains(text[index + function.Length]) == false))
							continue;

						classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, function.Length), functionType));
					}
				}

				// variables
				var matches = variables.Matches(text);
				foreach (Match match in matches)
					classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + match.Groups["Variable"].Index, match.Groups["Variable"].Length), variableType));
			}

			return classifiedSpans;
		}

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
		{
			add { }
			remove { }
		}
	}
}
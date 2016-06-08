// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.CommandLine
{
    class OutputTable
    {
        public static void Print<T>(TextWriter writer, IEnumerable<T> rows, Action<OutputTable<T>> tableInitializer)
        {
            var table = new OutputTable<T>();
            tableInitializer(table);
            table.Print(writer, rows);
        }
    }

    class OutputTable<TRow>
    {
        private readonly List<OutputColumn<TRow>> _columns;

        public OutputTable(params OutputColumn<TRow>[] columns)
        {
            _columns = columns.ToList();
        }

        public OutputTable<TRow> WithColumn(string header, Func<TRow, string> fieldRetriever, int columnWidth = 10)
        {
            _columns.Add(new OutputColumn<TRow>(header, fieldRetriever, columnWidth));
            return this;
        }

        public void Print(TextWriter writer, IEnumerable<TRow> rows)
        {
            var rowList = rows.ToList();
            foreach (var c in _columns)
            {
                c.SetColumnWidth(rowList);
            }

            PrintHeaders(writer);
            foreach (var row in rowList)
            {
                PrintRow(writer, row);
            }
        }

        public void PrintHeaders(TextWriter writer)
        {
            foreach (var c in _columns)
            {
                writer.Write(c.Header);
            }
            writer.WriteLine();

            foreach (var c in _columns)
            {
                writer.Write(new string('-', c.ColumnWidth - 1) + " ");
            }
            writer.WriteLine();
        }

        public void PrintRow(TextWriter writer, TRow row)
        {
            foreach (var c in _columns)
            {
                writer.Write(c.Row(row));
            }
            writer.WriteLine();
        }
    }

    class OutputColumn<T>
    {
        private readonly string _header;
        private int _columnWidth;
        private readonly Func<T, string> _fieldRetriever;

        public OutputColumn(string header, Func<T, string> fieldRetriever, int columnWidth = 10)
        {
            _header = header;
            _fieldRetriever = fieldRetriever;
            _columnWidth = columnWidth;
        }

        public void SetColumnWidth(IEnumerable<T> rows)
        {
            _columnWidth = _header.Length;
            _columnWidth = rows.Aggregate(_header.Length, (len, row) =>
            {
                int rowWidth = _fieldRetriever(row).Length;
                return len < rowWidth ? rowWidth : len;
            });
            ++_columnWidth;
        }

        public string Header
        {
            get { return _header.PadRight(_columnWidth); }
        }

        public int ColumnWidth
        {
            get { return _columnWidth; }
        }

        public string Row(T row)
        {
            return _fieldRetriever(row).PadRight(_columnWidth);
        }
    }
}

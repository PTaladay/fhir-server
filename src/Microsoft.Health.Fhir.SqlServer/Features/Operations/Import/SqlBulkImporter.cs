﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Fhir.Core.Features.Operations.Import;
using Microsoft.Health.Fhir.SqlServer.Features.Operations.Import;
using Microsoft.Health.Fhir.SqlServer.Features.Operations.Import.DataGenerator;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Fhir.SqlServer.Features.Operations.Reindex
{
    public class SqlBulkImporter : IBulkImporter<BulkImportResourceWrapper>
    {
        private const int MaxResourceCountInBatch = 10000;
        private const int MaxConcurrentCount = 10;

        private List<TableBulkCopyDataGenerator<SqlBulkCopyDataWrapper>> _generators = new List<TableBulkCopyDataGenerator<SqlBulkCopyDataWrapper>>();
        private SqlBulkCopyDataWrapperFactory _sqlBulkCopyDataWrapperFactory;
        private SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        internal SqlBulkImporter(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            SqlBulkCopyDataWrapperFactory sqlBulkCopyDataWrapperFactory,
            DateTimeSearchParamsTableBulkCopyDataGenerator dateTimeSearchParamsTableBulkCopyDataGenerator,
            NumberSearchParamsTableBulkCopyDataGenerator numberSearchParamsTableBulkCopyDataGenerator,
            QuantitySearchParamsTableBulkCopyDataGenerator quantitySearchParamsTableBulkCopyDataGenerator,
            ReferenceSearchParamsTableBulkCopyDataGenerator referenceSearchParamsTableBulkCopyDataGenerator,
            ReferenceTokenCompositeSearchParamsTableBulkCopyDataGenerator referenceTokenCompositeSearchParamsTableBulkCopyDataGenerator,
            StringSearchParamsTableBulkCopyDataGenerator stringSearchParamsTableBulkCopyDataGenerator,
            TokenDateTimeCompositeSearchParamsTableBulkCopyDataGenerator tokenDateTimeCompositeSearchParamsTableBulkCopyDataGenerator,
            TokenNumberNumberCompositeSearchParamsTableBulkCopyDataGenerator tokenNumberNumberCompositeSearchParamsTableBulkCopyDataGenerator,
            TokenQuantityCompositeSearchParamsTableBulkCopyDataGenerator tokenQuantityCompositeSearchParamsTableBulkCopyDataGenerator,
            TokenSearchParamsTableBulkCopyDataGenerator tokenSearchParamsTableBulkCopyDataGenerator,
            TokenStringCompositeSearchParamsTableBulkCopyDataGenerator tokenStringCompositeSearchParamsTableBulkCopyDataGenerator,
            TokenTextSearchParamsTableBulkCopyDataGenerator tokenTextSearchParamsTableBulkCopyDataGenerator,
            TokenTokenCompositeSearchParamsTableBulkCopyDataGenerator tokenTokenCompositeSearchParamsTableBulkCopyDataGenerator,
            UriSearchParamsTableBulkCopyDataGenerator uriSearchParamsTableBulkCopyDataGenerator)
        {
            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _sqlBulkCopyDataWrapperFactory = sqlBulkCopyDataWrapperFactory;

            _generators.Add(dateTimeSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(numberSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(quantitySearchParamsTableBulkCopyDataGenerator);
            _generators.Add(referenceSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(referenceTokenCompositeSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(stringSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(tokenDateTimeCompositeSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(tokenNumberNumberCompositeSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(tokenQuantityCompositeSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(tokenSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(tokenStringCompositeSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(tokenTextSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(tokenTokenCompositeSearchParamsTableBulkCopyDataGenerator);
            _generators.Add(uriSearchParamsTableBulkCopyDataGenerator);
        }

        public async Task ImportResourceAsync(Channel<BulkImportResourceWrapper> inputChannel, IProgress<(string tableName, long endSurrogateId)> progress,  CancellationToken cancellationToken)
        {
            Dictionary<string, DataTable> buffer = new Dictionary<string, DataTable>();
            Queue<Task<(string tableName, long endSurrogateId)>> runningTasks = new Queue<Task<(string, long)>>();

            long surrogateId = 0;
            while (await inputChannel.Reader.WaitToReadAsync(cancellationToken) && !cancellationToken.IsCancellationRequested)
            {
                await foreach (BulkImportResourceWrapper resource in inputChannel.Reader.ReadAllAsync())
                {
                    surrogateId = resource.ResourceSurrogateId;

                    SqlBulkCopyDataWrapper dataWrapper = _sqlBulkCopyDataWrapperFactory.CreateSqlBulkCopyDataWrapper(resource);

                    foreach (TableBulkCopyDataGenerator<SqlBulkCopyDataWrapper> generator in _generators)
                    {
                        if (!buffer.ContainsKey(generator.TableName))
                        {
                            buffer[generator.TableName] = generator.GenerateDataTable();
                        }

                        generator.FillDataTable(buffer[generator.TableName], dataWrapper);
                    }

                    string[] tableNameNeedImport = buffer.Where(r => r.Value.Rows.Count >= MaxResourceCountInBatch).Select(r => r.Key).ToArray();
                    foreach (string tableName in tableNameNeedImport)
                    {
                        while (runningTasks.Count() >= MaxConcurrentCount)
                        {
                            (string tableName, long endSurrogateId) result = await runningTasks.Dequeue();
                            progress.Report(result);
                        }

                        DataTable inputTable = buffer[tableName];
                        buffer.Remove(tableName);

                        runningTasks.Enqueue(BulkCopyAsync(inputTable, surrogateId, cancellationToken));
                    }
                }
            }

            string[] tableNames = buffer.Keys.ToArray();
            foreach (string tableName in tableNames)
            {
                DataTable inputTable = buffer[tableName];
                runningTasks.Enqueue(BulkCopyAsync(inputTable, surrogateId, cancellationToken));
            }

            while (runningTasks.Count() > 0 && !cancellationToken.IsCancellationRequested)
            {
                (string tableName, long endSurrogateId) result = await runningTasks.Dequeue();
                progress.Report(result);
            }
        }

        private async Task<(string tableName, long endSurrogateId)> BulkCopyAsync(DataTable inputTable, long endSurrogateId, CancellationToken cancellationToken)
        {
            using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken, true);
            using SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlConnectionWrapper.SqlConnection);
            bulkCopy.DestinationTableName = inputTable.TableName;

            await bulkCopy.WriteToServerAsync(inputTable.CreateDataReader());

            return (inputTable.TableName, endSurrogateId);
        }
    }
}
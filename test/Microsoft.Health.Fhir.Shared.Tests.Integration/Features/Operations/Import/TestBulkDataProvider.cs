﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using Microsoft.Health.Fhir.SqlServer.Features.Operations.Import.DataGenerator;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Fhir.Shared.Tests.Integration.Features.Operations.Import
{
    public static class TestBulkDataProvider
    {
        public static DataTable GenerateResourceTable(int count, long startSurrogatedId, short resoureType)
        {
            ResourceTableBulkCopyDataGenerator generator = new ResourceTableBulkCopyDataGenerator();

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                ResourceTableBulkCopyDataGenerator.FillDataTable(result, resoureType, Guid.NewGuid().ToString(), startSurrogatedId + i, new byte[10], string.Empty);
            }

            return result;
        }

        public static DataTable GenerateDateTimeSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            DateTimeSearchParamsTableBulkCopyDataGenerator generator = new DateTimeSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                DateTimeSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkDateTimeSearchParamTableTypeV1Row(0, 0, default(DateTimeOffset), default(DateTimeOffset), true));
            }

            return result;
        }

        public static DataTable GenerateNumberSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            NumberSearchParamsTableBulkCopyDataGenerator generator = new NumberSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                NumberSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkNumberSearchParamTableTypeV1Row(0, 0, 1, 1, 1));
            }

            return result;
        }

        public static DataTable GenerateQuantitySearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            QuantitySearchParamsTableBulkCopyDataGenerator generator = new QuantitySearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                QuantitySearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkQuantitySearchParamTableTypeV1Row(0, 0, 1, 1, 1, 1, 1));
            }

            return result;
        }

        public static DataTable GenerateReferenceSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            ReferenceSearchParamsTableBulkCopyDataGenerator generator = new ReferenceSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                ReferenceSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkReferenceSearchParamTableTypeV1Row(0, 0, string.Empty, 1, string.Empty, 1));
            }

            return result;
        }

        public static DataTable GenerateReferenceTokenCompositeSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            ReferenceTokenCompositeSearchParamsTableBulkCopyDataGenerator generator = new ReferenceTokenCompositeSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                ReferenceTokenCompositeSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkReferenceTokenCompositeSearchParamTableTypeV1Row(0, 0, string.Empty, 1, string.Empty, 1, 1, string.Empty));
            }

            return result;
        }

        public static DataTable GenerateStringSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            StringSearchParamsTableBulkCopyDataGenerator generator = new StringSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                StringSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkStringSearchParamTableTypeV1Row(0, 0, string.Empty, string.Empty));
            }

            return result;
        }

        public static DataTable GenerateTokenDateTimeCompositeSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            TokenDateTimeCompositeSearchParamsTableBulkCopyDataGenerator generator = new TokenDateTimeCompositeSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                TokenDateTimeCompositeSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkTokenDateTimeCompositeSearchParamTableTypeV1Row(0, 0, 1, string.Empty, default(DateTimeOffset), default(DateTimeOffset), true));
            }

            return result;
        }

        public static DataTable GenerateTokenNumberNumberCompositeSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            TokenNumberNumberCompositeSearchParamsTableBulkCopyDataGenerator generator = new TokenNumberNumberCompositeSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                TokenNumberNumberCompositeSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkTokenNumberNumberCompositeSearchParamTableTypeV1Row(0, 0, 1, string.Empty, 0, 0, 0, 0, 0, 0, true));
            }

            return result;
        }

        public static DataTable GenerateTokenQuantityCompositeSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            TokenQuantityCompositeSearchParamsTableBulkCopyDataGenerator generator = new TokenQuantityCompositeSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                TokenQuantityCompositeSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkTokenQuantityCompositeSearchParamTableTypeV1Row(0, 0, 0, string.Empty, 0, 0, 0, 0, 0));
            }

            return result;
        }

        public static DataTable GenerateTokenSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            TokenSearchParamsTableBulkCopyDataGenerator generator = new TokenSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                TokenSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkTokenSearchParamTableTypeV1Row(0, 0, 0, string.Empty));
            }

            return result;
        }

        public static DataTable GenerateTokenStringCompositeSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            TokenStringCompositeSearchParamsTableBulkCopyDataGenerator generator = new TokenStringCompositeSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                TokenStringCompositeSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkTokenStringCompositeSearchParamTableTypeV1Row(0, 0, 0, string.Empty, string.Empty, string.Empty));
            }

            return result;
        }

        public static DataTable GenerateTokenTextSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            TokenTextSearchParamsTableBulkCopyDataGenerator generator = new TokenTextSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                TokenTextSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkTokenTextTableTypeV1Row(0, 0, string.Empty));
            }

            return result;
        }

        public static DataTable GenerateTokenTokenCompositeSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            TokenTokenCompositeSearchParamsTableBulkCopyDataGenerator generator = new TokenTokenCompositeSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                TokenTokenCompositeSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkTokenTokenCompositeSearchParamTableTypeV1Row(0, 0, 0, string.Empty, 0, string.Empty));
            }

            return result;
        }

        public static DataTable GenerateUriSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            UriSearchParamsTableBulkCopyDataGenerator generator = new UriSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                UriSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkUriSearchParamTableTypeV1Row(default, 0, string.Empty));
            }

            return result;
        }

        public static DataTable GenerateCompartmentAssignmentTable(int count, long startSurrogatedId, short resoureType)
        {
            CompartmentAssignmentTableBulkCopyDataGenerator generator = new CompartmentAssignmentTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                CompartmentAssignmentTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkCompartmentAssignmentTableTypeV1Row(0, 1, string.Empty));
            }

            return result;
        }

        public static DataTable GenerateResourceWriteClaimTable(int count, long startSurrogatedId, short resoureType)
        {
            ResourceWriteClaimTableBulkCopyDataGenerator generator = new ResourceWriteClaimTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                ResourceWriteClaimTableBulkCopyDataGenerator.FillDataTable(result, startSurrogatedId + i, new ResourceWriteClaimTableTypeV1Row(1, string.Empty));
            }

            return result;
        }

        public static DataTable GenerateInValidUriSearchParamsTable(int count, long startSurrogatedId, short resoureType)
        {
            UriSearchParamsTableBulkCopyDataGenerator generator = new UriSearchParamsTableBulkCopyDataGenerator(null);

            DataTable result = generator.GenerateDataTable();

            for (int i = 0; i < count; ++i)
            {
                UriSearchParamsTableBulkCopyDataGenerator.FillDataTable(result, resoureType, startSurrogatedId + i, new BulkUriSearchParamTableTypeV1Row(default, 0, null));
            }

            return result;
        }
    }
}
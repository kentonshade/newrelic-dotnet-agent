﻿using NewRelic.Agent.Core.Utilities;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NewRelic.Agent.Core.WireModels
{
	[TestFixture, Category("ErrorEvents"), TestOf(typeof(ErrorEventWireModel))]
	public class ErrorEventWireModelTests
	{
		private const string TimeStampKey = "timestamp";

		[Test]
		public void All_attribute_value_types_in_an_event_do_serialize_correctly()
		{
			// ARRANGE
			var userAttributes = new ReadOnlyDictionary<String, Object>(new Dictionary<String, Object>
				{
					{"identity.user", "samw"},
					{"identity.product", "product"}
				});
			var agentAttributes = new ReadOnlyDictionary<String, Object>(new Dictionary<String, Object>
				{
					{"Foo", "Bar"},
					{"Baz", 42},
				});
			var intrinsicAttributes = new ReadOnlyDictionary<String, Object>(new Dictionary<String, Object>
				{
					{"databaseCallCount", 10 },
					{"errormessage", "This is the error message"},
					{"nr.pathHash", "DCBA4321"},
					{"nr.referringPathHash", "1234ABCD"},
					{"nr.referringTransactionGuid", "DCBA43211234ABCD"},
					{"nr.alternatePathHashes", "55f97a7f,6fc8d18f,72827114,9a3ed934,a1744603,a7d2798f,be1039f5,ccadfd2c,da7edf2e,eaca716b"},
				});

			var isSyntheticsEvent = false;

			// ACT
			float priority = 0.5f;
			var errorEventWireModel = new ErrorEventWireModel(agentAttributes, intrinsicAttributes, userAttributes, isSyntheticsEvent, priority);
			var actualResult = JsonConvert.SerializeObject(errorEventWireModel);

			// ASSERT
			const string expected = @"[{""databaseCallCount"":10,""errormessage"":""This is the error message"",""nr.pathHash"":""DCBA4321"",""nr.referringPathHash"":""1234ABCD"",""nr.referringTransactionGuid"":""DCBA43211234ABCD"",""nr.alternatePathHashes"":""55f97a7f,6fc8d18f,72827114,9a3ed934,a1744603,a7d2798f,be1039f5,ccadfd2c,da7edf2e,eaca716b""},{""identity.user"":""samw"",""identity.product"":""product""},{""Foo"":""Bar"",""Baz"":42}]";
			Assert.AreEqual(expected, actualResult);
		}

		[Test]
		public void Is_synthetics_set_correctly()
		{
			// Arrange
			var userAttributes = new ReadOnlyDictionary<String, Object>(new Dictionary<String, Object>());
			var agentAttributes = new ReadOnlyDictionary<String, Object>(new Dictionary<String, Object>());
			var intrinsicAttributes = new ReadOnlyDictionary<String, Object>(new Dictionary<String, Object>());
			var isSyntheticsEvent = true;

			// Act
			float priority = 0.5f;
			var errorEventWireModel = new ErrorEventWireModel(agentAttributes, intrinsicAttributes, userAttributes, isSyntheticsEvent, priority);

			// Assert
			Assert.IsTrue(errorEventWireModel.IsSynthetics());
		}

		[Test]
		public void Verify_setting_priority()
		{
			float priority = 0.5f;
			var emptyDictionary = new Dictionary<string, object>();
			var intrinsicAttributes = new Dictionary<String, Object> { { TimeStampKey, DateTime.UtcNow.ToUnixTime() } };
			var object1 = new ErrorEventWireModel(emptyDictionary, intrinsicAttributes, emptyDictionary, false, priority);

			Assert.That(priority == object1.Priority);

			priority = 0.0f;
			object1.Priority = priority;
			Assert.That(priority == object1.Priority);

			priority = 1.0f;
			object1.Priority = priority;
			Assert.That(priority == object1.Priority);

			priority = 1.1f;
			Assert.Throws<ArgumentException>(() => object1.Priority = priority);
			priority = -0.00001f;
			Assert.Throws<ArgumentException>(() => object1.Priority = priority);
			priority = float.NaN;
			Assert.Throws<ArgumentException>(() => object1.Priority = priority);
			priority = float.NegativeInfinity;
			Assert.Throws<ArgumentException>(() => object1.Priority = priority);
			priority = float.PositiveInfinity;
			Assert.Throws<ArgumentException>(() => object1.Priority = priority);
			priority = float.MaxValue;
			Assert.Throws<ArgumentException>(() => object1.Priority = priority);
			priority = float.MinValue;
			Assert.Throws<ArgumentException>(() => object1.Priority = priority);
		}

		[Test]
		public void Verify_comparer_operations()
		{
			var comparer = new ErrorEventWireModel.PriorityTimestampComparer();

			float priority = 0.5f;
			var emptyDictionary = new Dictionary<string, object>();
			var intrinsicAttributes1 = new Dictionary<String, Object> { { TimeStampKey, DateTime.UtcNow.ToUnixTime() } };
			Thread.Sleep(1);
			var intrinsicAttributes2 = new Dictionary<String, Object> { { TimeStampKey, DateTime.UtcNow.ToUnixTime() } };

			//same priority, same timestamp
			var object1 = new ErrorEventWireModel(emptyDictionary, intrinsicAttributes1, emptyDictionary, false, priority);
			var object2 = new ErrorEventWireModel(emptyDictionary, intrinsicAttributes1, emptyDictionary, false, priority);
			Assert.True(0 == comparer.Compare(object1, object2));
			//same priority, timestamp later
			var object3 = new ErrorEventWireModel(emptyDictionary, intrinsicAttributes2, emptyDictionary, false, priority);
			//same priority, object1.timestamp < object2.timestamp
			Assert.True(-1 == comparer.Compare(object1, object3));
			//same priority, object3.timestamp > object1.timestamp
			Assert.True(1 == comparer.Compare(object3, object1));

			var object4 = new ErrorEventWireModel(emptyDictionary, emptyDictionary, emptyDictionary, false, priority);
			//x param does not have a timestamp
			var ex = Assert.Throws<ArgumentException>(() => comparer.Compare(object4, object1));
			Assert.That(ex.ParamName == "x");

			//y param does not have a timestamp
			ex = Assert.Throws<ArgumentException>(() => comparer.Compare(object1, object4));
			Assert.That(ex.ParamName == "y");

			Assert.True(1 == comparer.Compare(object1, null));
			Assert.True(-1 == comparer.Compare(null, object1));
			Assert.True(0 == comparer.Compare(null, null));
		}

	}
}

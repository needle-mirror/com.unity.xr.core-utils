using System.Collections.Generic;
using NUnit.Framework;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;

namespace Unity.XR.CoreUtils.EditorTests
{
    class ReadOnlyListTests
    {
        [Test]
        public void ToString_SucceedsWithNullItemsInList()
        {
            var list = new List<object> { 1, null, 2 };
            var listReadOnly = new ReadOnlyList<object>(list);
            Debug.Log(listReadOnly.ToString());
            // Test passes if no errors are logged
        }
    }
}

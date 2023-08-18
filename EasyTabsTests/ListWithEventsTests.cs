using EasyTabs.Model;
using NUnit.Framework;

namespace EasyTabsTests
{
    [TestFixture]
    public class ListWithEventsTests
    {
        [Test]
        public void AddItem_ItemAddedEventRaised()
        {
            ListWithEvents<int> list = new ListWithEvents<int>();
            bool eventRaised = false;

            list.ItemAdded += (_, _) => eventRaised = true;

            list.Add(42);

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void ModifyItem_ItemModifiedEventRaised()
        {
            ListWithEvents<string> list = new ListWithEvents<string>();
            bool eventRaised = false;

            list.Add("Hello");
            list.ItemModified += (_, _) => eventRaised = true;

            list[0] = "World";

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void RemoveItem_ItemRemovedEventRaised()
        {
            ListWithEvents<double> list = new ListWithEvents<double> { 1.0, 2.0, 3.0 };
            bool eventRaised = false;

            list.ItemRemoved += (_, _) => eventRaised = true;

            list.Remove(2.0);

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void ClearList_ClearedEventRaised()
        {
            ListWithEvents<int> list = new ListWithEvents<int> { 1, 2, 3 };
            bool eventRaised = false;

            list.Cleared += (_, _) => eventRaised = true;

            list.Clear();

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void AddRange_RangeAddedEventRaised()
        {
            ListWithEvents<string> list = new ListWithEvents<string>();
            bool eventRaised = false;

            list.RangeAdded += (_, _) => eventRaised = true;

            list.AddRange(new[] { "apple", "banana", "cherry" });

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void RemoveAll_ItemsRemovedEventRaised()
        {
            ListWithEvents<int> list = new ListWithEvents<int> { 1, 2, 3, 2, 4, 2, 5 };
            bool eventRaised = false;

            list.RangeRemoved += (_, _) => eventRaised = true;

            int removedCount = list.RemoveAll(item => item == 2);

            Assert.IsTrue(eventRaised);
            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(4, list.Count); // After removing 3 occurrences of '2'
        }


        [Test]
        public void InsertItem_ItemAddedEventRaised()
        {
            ListWithEvents<string> list = new ListWithEvents<string> { "apple", "banana", "cherry" };
            bool eventRaised = false;

            list.ItemAdded += (_, _) => eventRaised = true;

            list.Insert(1, "grape");

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void InsertRange_RangeAddedEventRaised()
        {
            ListWithEvents<int> list = new ListWithEvents<int> { 1, 2, 3 };
            bool eventRaised = false;

            list.RangeAdded += (_, _) => eventRaised = true;

            list.InsertRange(1, new[] { 4, 5 });

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void RemoveRange_RangeRemovedEventRaised()
        {
            ListWithEvents<int> list = new ListWithEvents<int> { 1, 2, 3, 4, 5 };
            bool eventRaised = false;

            list.RangeRemoved += (_, _) => eventRaised = true;

            list.RemoveRange(1, 3);

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void SuppressAndResumeEvents_EventsNotRaised()
        {
            ListWithEvents<int> list = new ListWithEvents<int> { 1, 2, 3 };
            bool eventRaised = false;

            list.ItemAdded += (_, _) => eventRaised = true;

            list.SuppressEvents();
            list.Add(4);
            list.ResumeEvents();

            Assert.IsFalse(eventRaised);
        }
    }
}

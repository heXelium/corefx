// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class EventsTests : XLinqTestCase
        {
            public partial class EventsXElementName : EventsBase
            {
                protected override void DetermineChildren()
                {
                    VariationsForXElement(ExecuteXElementVariation);
                    base.DetermineChildren();
                }

                void VariationsForXElement(TestFunc func)
                {
                    AddChild(func, 0, "XElement - Empty element without namespace", new XElement("element"), (XName)"newName");
                    AddChild(func, 0, "XElement - Element with namespace", new XElement("parent", new XElement("child", "child text")), (XName)"{b}newName");
                }

                public void ExecuteXElementVariation()
                {
                    XElement toChange = Variation.Params[0] as XElement;
                    XName newName = (XName)Variation.Params[1];
                    XElement original = new XElement(toChange);
                    using (UndoManager undo = new UndoManager(toChange))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(toChange))
                        {
                            toChange.Name = newName;
                            TestLog.Compare(newName.Namespace == toChange.Name.Namespace, "Namespace did not change");
                            TestLog.Compare(newName.LocalName == toChange.Name.LocalName, "LocalName did not change");
                            eHelper.Verify(XObjectChange.Name, toChange);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(toChange, original), "Undo did not work");
                    }
                }

                //[Variation(Priority = 0, Desc = "XProcessingInstruction - Name")]
                public void PIVariation()
                {
                    XProcessingInstruction toChange = new XProcessingInstruction("target", "data");
                    XProcessingInstruction original = new XProcessingInstruction(toChange);
                    using (UndoManager undo = new UndoManager(toChange))
                    {
                        undo.Group();
                        using (EventsHelper eHelper = new EventsHelper(toChange))
                        {
                            toChange.Target = "newTarget";
                            TestLog.Compare(toChange.Target.Equals("newTarget"), "Name did not change");
                            eHelper.Verify(XObjectChange.Name, toChange);
                        }
                        undo.Undo();
                        TestLog.Compare(XNode.DeepEquals(toChange, original), "Undo did not work");
                    }
                }

                //[Variation(Priority = 0, Desc = "XDocumentType - Name")]
                public void DocTypeVariation()
                {
                    XDocumentType toChange = new XDocumentType("root", "", "", "");
                    XDocumentType original = new XDocumentType(toChange);
                    using (EventsHelper eHelper = new EventsHelper(toChange))
                    {
                        toChange.Name = "newName";
                        TestLog.Compare(toChange.Name.Equals("newName"), "Name did not change");
                        eHelper.Verify(XObjectChange.Name, toChange);
                    }
                }
            }

            public partial class EventsSpecialCases : EventsBase
            {
                public void ChangingDelegate(object sender, XObjectChangeEventArgs e) { }
                public void ChangedDelegate(object sender, XObjectChangeEventArgs e) { }

                //[Variation(Priority = 1, Desc = "Adding, Removing null listeners")]
                public void XElementRemoveNullEventListner()
                {
                    XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
                    EventHandler<XObjectChangeEventArgs> d1 = ChangingDelegate;
                    EventHandler<XObjectChangeEventArgs> d2 = ChangedDelegate;
                    //Add null first, this should add nothing
                    xDoc.Changing += null;
                    xDoc.Changed += null;
                    //Add the actuall delegate
                    xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(d1);
                    xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(d2);
                    //Now set it to null
                    d1 = null;
                    d2 = null;
                    xDoc.Root.Add(new XComment("This is a comment"));
                    //Remove nulls
                    xDoc.Changing -= null;
                    xDoc.Changed -= null;
                    //Try removing the originally added delegates
                    d1 = ChangingDelegate;
                    d2 = ChangedDelegate;
                    xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
                    xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
                }

                //[Variation(Priority = 1, Desc = "Remove both event listeners")]
                public void XElementRemoveBothEventListners()
                {
                    XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
                    EventHandler<XObjectChangeEventArgs> d1 = ChangingDelegate;
                    EventHandler<XObjectChangeEventArgs> d2 = ChangedDelegate;
                    xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(d1);
                    xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(d2);
                    xDoc.Root.Add(new XComment("This is a comment"));
                    xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
                    xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
                    //Remove it again
                    xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
                    xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
                    //Change the order
                    xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(d2);
                    xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(d1);
                    xDoc.Root.Add(new XComment("This is a comment"));
                    xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
                    xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
                    //Remove it again
                    xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
                    xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
                }

                //[Variation(Priority = 1, Desc = "Add Changed listner in pre-event")]
                public void AddListnerInPreEvent()
                {
                    XElement element = XElement.Parse("<root></root>");
                    element.Changing += new EventHandler<XObjectChangeEventArgs>(
                        delegate (object sender, XObjectChangeEventArgs e)
                        {
                            element.Changed += new EventHandler<XObjectChangeEventArgs>(ChangedDelegate);
                        });

                    element.Add(new XElement("Add", "Me"));
                    element.Verify();
                    TestLog.Compare(element.Element("Add") != null, "Did not add the element");
                }

                //[Variation(Priority = 1, Desc = "Add and remove event listners")]
                public void XElementAddRemoveEventListners()
                {
                    XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
                    EventsHelper docHelper = new EventsHelper(xDoc);
                    EventsHelper eHelper = new EventsHelper(xDoc.Root);
                    xDoc.Root.Add(new XElement("Add", "Me"));
                    docHelper.Verify(XObjectChange.Add);
                    eHelper.Verify(XObjectChange.Add);
                    eHelper.RemoveListners();
                    xDoc.Root.Add(new XComment("Comment"));
                    eHelper.Verify(0);
                    docHelper.Verify(XObjectChange.Add);
                }

                //[Variation(Priority = 1, Desc = "Attach listners at each level, nested elements")]
                public void XElementAttachAtEachLevel()
                {
                    XDocument xDoc = new XDocument(XElement.Parse(@"<a>a<b>b<c>c<d>c<e>e<f>f</f></e></d></c></b></a>"));
                    EventsHelper[] listeners = new EventsHelper[xDoc.Descendants().Count()];

                    int i = 0;
                    foreach (XElement x in xDoc.Descendants())
                        listeners[i++] = new EventsHelper(x);
                    // f element
                    XElement toChange = xDoc.Descendants().ElementAt(5);
                    // Add element
                    toChange.Add(new XElement("Add", "Me"));
                    foreach (EventsHelper e in listeners)
                        e.Verify(XObjectChange.Add);
                    // Add xattribute
                    toChange.Add(new XAttribute("at", "value"));
                    foreach (EventsHelper e in listeners)
                        e.Verify(XObjectChange.Add);
                }

                //[Variation(Priority = 2, Desc = "Exception in PRE Event Handler")]
                public void XElementPreException()
                {
                    XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
                    XElement toChange = xDoc.Descendants().ElementAt(5);

                    xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(
                        delegate (object sender, XObjectChangeEventArgs e)
                        {
                            throw new InvalidOperationException("This should be propagated and operation should be aborted");
                        });

                    xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(
                       delegate (object sender, XObjectChangeEventArgs e)
                       {
                           // This should never be called
                       });

                    try
                    {
                        toChange.Add(new XElement("Add", "Me"));
                    }
                    catch (InvalidOperationException)
                    {
                        xDoc.Root.Verify();
                        TestLog.Compare(toChange.Element("Add") == null, "Added the element, operation should have been aborted");
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }

                //[Variation(Priority = 2, Desc = "Exception in POST Event Handler")]
                public void XElementPostException()
                {
                    XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
                    XElement toChange = xDoc.Descendants().ElementAt(5);

                    xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(
                        delegate (object sender, XObjectChangeEventArgs e)
                        {
                            // Do nothing
                        });

                    xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(
                       delegate (object sender, XObjectChangeEventArgs e)
                       {
                           throw new InvalidOperationException("This should be propagated and operation should perform");
                       });

                    try
                    {
                        toChange.Add(new XElement("Add", "Me"));
                    }
                    catch (InvalidOperationException)
                    {
                        xDoc.Root.Verify();
                        TestLog.Compare(toChange.Element("Add") != null, "Did not add the element, operation should have completed");
                        return;
                    }
                    throw new TestException(TestResult.Failed, "");
                }
            }
        }
    }
}
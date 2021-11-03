﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Networking;
using NuGet.Frameworks;

namespace Testing.Networking
{
    [TestFixture]
    public class QueueManagement
    {
        private IQueue _queue;
        private List<Packet> _testPackets;
        private List<string> _moduleIdentifiers;

        [SetUp]
        public void Setup()
        {
            _queue = new Queue();
            _testPackets = new List<Packet>(100);
            _moduleIdentifiers = new List<string>(4);
            
            const string screenShareModuleId = "S";
            const string whiteBoardModuleId = "W";
            const string chatModuleId = "C";
            const string fileModuleId = "F";
            
            _moduleIdentifiers.Add(screenShareModuleId);
            _moduleIdentifiers.Add(whiteBoardModuleId);
            _moduleIdentifiers.Add(chatModuleId);
            _moduleIdentifiers.Add(fileModuleId);

            const int screenSharePriority = 4;
            const int whiteBoardPriority = 3;
            const int chatPriority = 2;
            const int filePriority = 1;
            
            _queue.RegisterModule(screenShareModuleId, screenSharePriority);
            _queue.RegisterModule(whiteBoardModuleId, whiteBoardPriority);
            _queue.RegisterModule(chatModuleId, chatPriority);
            _queue.RegisterModule(fileModuleId, filePriority);

            string tempModuleId;
            Random random = new Random();
            int moduleIndex;
            string tempData = "tempData";
            
            // Creating packets with random moduleIdentifiers
            for (int i = 0; i < _testPackets.Capacity; i++)
            {
                moduleIndex = random.Next(0,4);
                tempModuleId = _moduleIdentifiers[moduleIndex];
                Packet item = new Packet(tempModuleId, tempData);
                _testPackets.Add(item);
            }

        }
        
        [TearDown]
        public void TearDown()
        {
            // Dereference objects after the end of each test
            _moduleIdentifiers = null;
            _queue = null;
            _testPackets = null;
        }
        
        [Test]
        public void Enqueue_SinglePacket_SizeShouldBeOne()
        {
            const string moduleId = "S";
            const string data = "testData";
            int size = 0;
            Packet packet = new Packet(moduleId, data);
            
            Task thread = Task.Run(() =>
            {
                _queue.Enqueue(packet);
            });
            
            Task.WaitAll(thread);
            size = _queue.Size();
            Assert.AreEqual(1, size);
        }

        [Test]
        public void Enqueue_InvalidModuleIdentifier_ThrowsException()
        {
            const string moduleId = "A";
            const string data = "testData";
            Packet packet = new Packet(moduleId, data);

            Exception ex = Assert.Throws<Exception>(() =>
            {
                _queue.Enqueue(packet);
            });
            
            Assert.IsNotNull(ex);
            string expectedMessage = "Key Error: Packet holds invalid module identifier";
            Assert.AreEqual(expectedMessage, ex.Message);
        }

        [Test]
        public void Enqueue_MultiplePackets_SizeShouldMatch()
        {
            int size = 0;
            
            // Running enqueue from different threads
            Task thread1 = Task.Run(() =>
            {
                for (int i = 0; i < _testPackets.Capacity/2; i++)
                {
                    Packet packet = _testPackets[i];
                    _queue.Enqueue(packet);
                }
            });

            Task thread2 = Task.Run(() =>
            {
                for (int i = _testPackets.Capacity/2; i < _testPackets.Capacity; i++)
                {
                    Packet packet = _testPackets[i];
                    _queue.Enqueue(packet);
                }
            });

            Task.WaitAll(thread1, thread2);
            size = _queue.Size();
            int expectedSize = _testPackets.Count;
            Assert.AreEqual(expectedSize, size);
        }

        [Test]
        public void Clear_FlushingAllPackets_ShouldBeEmpty()
        {
            bool empty;
            Task thread1 = Task.Run(() =>
            {
                for (int i = 0; i < _testPackets.Capacity; i++)
                {
                    Packet packet = _testPackets[i];
                    _queue.Enqueue(packet);
                }
            });
            
            Task.WaitAll(thread1);

            Task thread2 = Task.Run(() =>
            {
                _queue.Clear();
            });
            
            Task.WaitAll(thread2);

            empty = _queue.IsEmpty();
            Assert.AreEqual(true, empty);
        }

        [Test]
        public void Clear_CalledByMultipleThreadsAtSameTime_ThrowsEmptyQueueException()
        {
            
            bool empty;
            Task thread1 = Task.Run(() =>
            {
                for (int i = 0; i < _testPackets.Capacity; i++)
                {
                    Packet packet = _testPackets[i];
                    _queue.Enqueue(packet);
                }
            });
            Task.WaitAll(thread1);
            
            AggregateException ex = Assert.Throws<AggregateException>(() =>
            {
                Task thread2 = Task.Run(() =>
                {
                    _queue.Clear();
                });
                Task thread3 = Task.Run(() =>
                {
                    _queue.Clear();
                });
                Task.WaitAll(thread2, thread3);
            });

            Assert.IsNotNull(ex);
            ReadOnlyCollection<Exception> innerEx = ex.InnerExceptions;
            Exception clearEx = innerEx.ElementAt(0);

            string expectedMessage = "Empty Queue cannot be dequeued";
            
            empty = _queue.IsEmpty();
            Assert.AreEqual(expectedMessage, clearEx.Message);
            Assert.AreEqual(true, empty);
        }

        [Test]
        public void RegisterModule_DifferentModulesPassingSameIdentifier_ThrowsException()
        {
            string moduleId = "A";
            int priority = 1;
            
            AggregateException ex = Assert.Throws<AggregateException>(() =>
            {
                Task thread1 = Task.Run(() =>
                {
                    _queue.RegisterModule(moduleId, priority);
                   
                });

                Task thread2 = Task.Run(() =>
                {
                    _queue.RegisterModule(moduleId, priority);
                });
                
                Task.WaitAll(thread1, thread2);
            });
            
            Assert.IsNotNull(ex);
            ReadOnlyCollection<Exception> innerEx = ex.InnerExceptions;
            Exception registerEx = innerEx.ElementAt(0);

            string expectedMessage = "Adding Queue to MultiLevelQueue Failed!";
            Assert.AreEqual(expectedMessage, registerEx.Message);
        }
        
        [Test]
        public void RegisterModule_IncorrectPriority_ThrowsException()
        {
            string moduleId = "A";
            int priority = -1;
            
            AggregateException ex = Assert.Throws<AggregateException>(() =>
            {
                Task thread = Task.Run(() =>
                {
                    _queue.RegisterModule(moduleId, priority);
                   
                });

                Task.WaitAll(thread);
            });
            
            Assert.IsNotNull(ex);
            ReadOnlyCollection<Exception> innerEx = ex.InnerExceptions;
            Exception registerEx = innerEx.ElementAt(0);

            string expectedMessage = "Priority should be positive integer";
            Assert.AreEqual(expectedMessage, registerEx.Message);
        }

        [Test]
        public void Dequeue_QueueIsEmpty_ThrowsException()
        {
            AggregateException ex = Assert.Throws<AggregateException>(() =>
            {
                Task thread = Task.Run(() =>
                {
                    _queue.Dequeue();
                });
                Task.WaitAll(thread);
            });
            
            Assert.IsNotNull(ex);
            ReadOnlyCollection<Exception> innerEx = ex.InnerExceptions;
            Exception dequeueEx = innerEx.ElementAt(0);

            string expectedMessage = "Cannot Dequeue empty queue";
            Assert.AreEqual(expectedMessage, dequeueEx.Message);
        }

        [Test]
        public void Peek_QueueIsEmpty_ThrowsException()
        {
            AggregateException ex = Assert.Throws<AggregateException>(() =>
            {
                Task thread = Task.Run(() =>
                {
                    _queue.Peek();
                });
                Task.WaitAll(thread);
            });
            
            Assert.IsNotNull(ex);
            ReadOnlyCollection<Exception> innerEx = ex.InnerExceptions;
            Exception dequeueEx = innerEx.ElementAt(0);

            string expectedMessage = "Cannot Peek into empty queue";
            Assert.AreEqual(expectedMessage, dequeueEx.Message);
        }

        [Test]
        public void Dequeue_SinglePacket_QueueIsEmpty()
        {
            const string moduleId = "S";
            const string data = "testData";
            int size = 8;
            Packet packet = new Packet(moduleId, data);
            
            Task thread1 = Task.Run(() =>
            {
                _queue.Enqueue(packet);
            });

            Task thread2 = Task.Run(() =>
            {
                Packet p = _queue.Dequeue();
                Assert.AreEqual(p.ModuleIdentifier, moduleId);
                Assert.AreEqual(p.SerializedData, data);
            });

            Task.WaitAll(thread1, thread2);

            size = _queue.Size();
            Assert.AreEqual(0, size);
        }

        [Test]
        public void Dequeue_MultiplePackets_SizeIsNotZero()
        {
            int size = 0;
            Task thread1 = Task.Run(() =>
            {
                for (int i = 0; i < _testPackets.Capacity; i++)
                {
                    Packet packet = _testPackets[i];
                    _queue.Enqueue(packet);
                }
            });

            Task thread2 = Task.Run(() =>
            {
                for (int i = 0; i < _testPackets.Capacity; i++)
                {
                    Packet packet = _queue.Dequeue();
                    Console.WriteLine(packet.ModuleIdentifier);
                }
            });

            Task.WaitAll(thread1, thread2);

            size = _queue.Size();
            Assert.AreEqual(0, size);
        }
        
        [Test]
        public void Peek_SinglePacket_ShouldPeekSamePacket()
        {
            const string moduleId = "S";
            const string data = "testData";
            Packet packet = new Packet(moduleId, data);
            
            Task thread1 = Task.Run(() =>
            {
                _queue.Enqueue(packet);
            });

            Task thread2 = Task.Run(() =>
            {
                Packet p = _queue.Peek();
                Assert.AreEqual(p.ModuleIdentifier, moduleId);
                Assert.AreEqual(p.SerializedData, data);
            });

            Task.WaitAll(thread1, thread2);
        }
        
        [Test]
        public void Dequeue_CheckingOrder_ReturnsProperOrder()
        {
            const string moduleId1 = "X";
            const string xData1 = "xData1";
            const string xData2 = "xData2";
            const string xData3 = "xData3";

            const string moduleId2 = "Y";
            const string yData1 = "yData1";
            const string yData2 = "yData2";

            _queue.RegisterModule(moduleId1, 2);
            _queue.RegisterModule(moduleId2,  1);
            
            Packet xPacket1 = new Packet(moduleId1, xData1);
            Packet xPacket2 = new Packet(moduleId1, xData2);
            Packet xPacket3 = new Packet(moduleId1, xData3);
            
            Packet yPacket1 = new Packet(moduleId2, yData1);
            Packet yPacket2 = new Packet(moduleId2, yData2);
            

            Task thread1 = Task.Run(() =>
            {
                _queue.Enqueue(xPacket1);
                _queue.Enqueue(xPacket2);
                _queue.Enqueue(xPacket3);

            });

            Task thread2 = Task.Run(() =>
            {
                _queue.Enqueue(yPacket1);
                _queue.Enqueue(yPacket2);
            });

            Task thread3 = Task.Run(() =>
            {
                Packet p1 = _queue.Dequeue();
                Assert.AreEqual(moduleId1, p1.ModuleIdentifier);
                Assert.AreEqual(xData1, p1.SerializedData);

                Packet p2 = _queue.Dequeue();
                Assert.AreEqual(moduleId1, p2.ModuleIdentifier);
                Assert.AreEqual(xData2, p2.SerializedData);

                Packet p3 = _queue.Dequeue();
                Assert.AreEqual(moduleId2, p3.ModuleIdentifier);
                Assert.AreEqual(yData1, p3.SerializedData);
                
                Packet p4 = _queue.Dequeue();
                Assert.AreEqual(moduleId1, p4.ModuleIdentifier);
                Assert.AreEqual(xData3, p4.SerializedData);
                
                Packet p5 = _queue.Dequeue();
                Assert.AreEqual(moduleId2, p5.ModuleIdentifier);
                Assert.AreEqual(yData2, p5.SerializedData);
            });

            Task.WaitAll(thread1, thread2, thread3);
        }

        [Test]
        public void Dequeue_RegisteringModuleAmidst_ShouldNotAlterDequeueFunctionality()
        {
            const string moduleId = "S";
            const string data = "screenData";

            const string moduleId2 = "C";
            const string data2 = "chatData";

            const string newModuleId = "X";
            const string newData = "xData";
            int newPriority = 5;

            Packet packet1 = new Packet(moduleId, data);
            Packet packet2 = new Packet(moduleId2, data2);
            Packet packet3 = new Packet(newModuleId, newData);
            
            Task thread1 = Task.Run(() =>
            {
                _queue.Enqueue(packet1);
            });

            Task thread2 = Task.Run(() =>
            {
                _queue.Enqueue(packet2);
            });
            
            Task.WaitAll(thread1, thread2);

            Task thread3 = Task.Run(() =>
            {
                Packet p1 = _queue.Dequeue();
                Assert.AreEqual(moduleId, p1.ModuleIdentifier);
                Assert.AreEqual(data, p1.SerializedData);
            });

            Task thread4 = Task.Run(() =>
            {
                _queue.RegisterModule(newModuleId, newPriority);
            });
            
            Task.WaitAll(thread3, thread4);

            Task thread5 = Task.Run(() =>
            {
                _queue.Enqueue(packet3);
            });

            Task thread6 = Task.Run(() =>
            {
                Packet p2 = _queue.Dequeue();
                Assert.AreEqual(moduleId2, p2.ModuleIdentifier);
                Assert.AreEqual(data2, p2.SerializedData);

                Packet p3 = _queue.Dequeue();
                Assert.AreEqual(newModuleId, p3.ModuleIdentifier);
                Assert.AreEqual(newData, p3.SerializedData);
            });

            Task.WaitAll(thread5, thread6);
        }
    }
}
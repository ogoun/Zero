using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DOM.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.Network;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.UnitTests
{
    [TestClass]
    public class SerializationTests
    {
        private static bool TestOrderingEquals<T>(IEnumerable<T> A, IEnumerable<T> B, Func<T, T, bool> comparer)
        {
            if (A == null && B == null) return true;
            if (A == null || B == null) return false;
            if (A.Count() != B.Count()) return false;
            var enumA = A.GetEnumerator();
            var enumB = B.GetEnumerator();
            while (enumA.MoveNext() && enumB.MoveNext())
            {
                if (enumA.Current == null && enumB.Current == null) continue;
                if (comparer(enumA.Current, enumB.Current) == false) return false;
            }
            return true;
        }

        private void MakePrimitiveTest<T>(T value, Func<T, T, bool> comparator = null)
        {
            // Act
            var data = MessageSerializer.SerializeCompatible<T>(value);
            var clone = MessageSerializer.DeserializeCompatible<T>(data);

            // Assert
            if (comparator == null)
            {
                Assert.AreEqual<T>(value, clone);
            }
            else
            {
                Assert.IsTrue(comparator(value, clone));
            }
        }

        private void MakeCollectionTest<T>(IEnumerable<T> value, Func<T, T, bool> comparator = null)
        {
            // Act
            var data = MessageSerializer.SerializeCompatible<IEnumerable<T>>(value);
            var clone = MessageSerializer.DeserializeCompatible<IEnumerable<T>>(data);

            // Assert
            if (value == null && clone != null && !clone.Any()) return; // OK
            if (comparator == null)
            {
                Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(value, clone));
            }
            else
            {
                Assert.IsTrue(TestOrderingEquals(value, clone, comparator));
            }
        }

        [TestMethod]
        public void SerializeDateTime()
        {
            MakePrimitiveTest<DateTime>(DateTime.Now);
            MakePrimitiveTest<DateTime>(DateTime.UtcNow);
            MakePrimitiveTest<DateTime>(DateTime.Today);
            MakePrimitiveTest<DateTime>(DateTime.Now.AddYears(2000));
            MakePrimitiveTest<DateTime>(DateTime.MinValue);
            MakePrimitiveTest<DateTime>(DateTime.MaxValue);
        }

        [TestMethod]
        public void SerializeIPAddress()
        {
            var comparator = new Func<IPAddress, IPAddress, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakePrimitiveTest<IPAddress>(IPAddress.Any, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Broadcast, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.IPv6Any, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.IPv6Loopback, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.IPv6None, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Loopback, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.None, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Parse("93.111.16.58"), comparator);
        }

        [TestMethod]
        public void SerializeIPEndPoint()
        {
            var comparator = new Func<IPEndPoint, IPEndPoint, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Any, 1), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Broadcast, 600), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MaxPort), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6Loopback, 8080), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6None, 80), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Loopback, 9000), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.None, 0), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Parse("93.111.16.58"), IPEndPoint.MinPort), comparator);
        }

        [TestMethod]
        public void SerializeGuid()
        {
            MakePrimitiveTest<Guid>(Guid.Empty);
            MakePrimitiveTest<Guid>(Guid.NewGuid());
        }

        [TestMethod]
        public void SerializeTimeSpan()
        {
            MakePrimitiveTest<TimeSpan>(TimeSpan.MaxValue);
            MakePrimitiveTest<TimeSpan>(TimeSpan.MinValue);
            MakePrimitiveTest<TimeSpan>(TimeSpan.Zero);
            MakePrimitiveTest<TimeSpan>(TimeSpan.FromDays(1024));
            MakePrimitiveTest<TimeSpan>(TimeSpan.FromMilliseconds(1));
            MakePrimitiveTest<TimeSpan>(TimeSpan.FromTicks(1));
            MakePrimitiveTest<TimeSpan>(TimeSpan.FromTicks(0));
        }

        [TestMethod]
        public void SerializeString()
        {
            var comparator = new Func<string, string, bool>((left, right) =>
                    (left == null && right == null) ||
                    (left == null && right != null && right.Length == 0) ||
                    (left != null && left.Length == 0 && right == null) ||
                    string.Compare(left, right, StringComparison.InvariantCulture) == 0);
            MakePrimitiveTest<String>("", comparator);
            MakePrimitiveTest<String>(String.Empty, comparator);
            MakePrimitiveTest<String>(null, comparator);
            MakePrimitiveTest<String>("HELLO!", comparator);
            MakePrimitiveTest<String>("𐌼𐌰𐌲 𐌲𐌻𐌴𐍃 𐌹̈𐍄𐌰𐌽, 𐌽𐌹 𐌼𐌹𐍃 𐍅𐌿 𐌽𐌳𐌰𐌽 𐌱𐍂𐌹𐌲𐌲𐌹𐌸", comparator);
        }

        [TestMethod]
        public void SerializeInt32()
        {
            MakePrimitiveTest<Int32>(-0);
            MakePrimitiveTest<Int32>(0);
            MakePrimitiveTest<Int32>(-10);
            MakePrimitiveTest<Int32>(10);
            MakePrimitiveTest<Int32>(Int32.MinValue);
            MakePrimitiveTest<Int32>(Int32.MaxValue);
        }

        [TestMethod]
        public void SerializeInt64()
        {
            MakePrimitiveTest<Int64>(-0);
            MakePrimitiveTest<Int64>(0);
            MakePrimitiveTest<Int64>(-10);
            MakePrimitiveTest<Int64>(10);
            MakePrimitiveTest<Int64>(Int64.MinValue);
            MakePrimitiveTest<Int64>(Int64.MaxValue);
            MakePrimitiveTest<Int64>(Int64.MinValue / 2);
            MakePrimitiveTest<Int64>(Int64.MaxValue / 2);
        }

        [TestMethod]
        public void SerializeDecimal()
        {
            MakePrimitiveTest<Decimal>(-0);
            MakePrimitiveTest<Decimal>(0);
            MakePrimitiveTest<Decimal>(-10);
            MakePrimitiveTest<Decimal>(10);
            MakePrimitiveTest<Decimal>(Decimal.MinValue);
            MakePrimitiveTest<Decimal>(Decimal.MaxValue);
            MakePrimitiveTest<Decimal>(Decimal.MinValue / 2);
            MakePrimitiveTest<Decimal>(Decimal.MaxValue / 2);
        }

        [TestMethod]
        public void SerializeFloat()
        {
            MakePrimitiveTest<float>(-0);
            MakePrimitiveTest<float>(0);
            MakePrimitiveTest<float>(-10);
            MakePrimitiveTest<float>(10);
            MakePrimitiveTest<float>(float.MinValue);
            MakePrimitiveTest<float>(float.MaxValue);
            MakePrimitiveTest<float>(float.MinValue / 2);
            MakePrimitiveTest<float>(float.MaxValue / 2);
        }

        [TestMethod]
        public void SerializeDouble()
        {
            MakePrimitiveTest<Double>(-0);
            MakePrimitiveTest<Double>(0);
            MakePrimitiveTest<Double>(-10);
            MakePrimitiveTest<Double>(10);
            MakePrimitiveTest<Double>(Double.MinValue);
            MakePrimitiveTest<Double>(Double.MaxValue);
            MakePrimitiveTest<Double>(Double.MinValue / 2);
            MakePrimitiveTest<Double>(Double.MaxValue / 2);
        }

        [TestMethod]
        public void SerializeBoolean()
        {
            MakePrimitiveTest<Boolean>(true);
            MakePrimitiveTest<Boolean>(false);
        }

        [TestMethod]
        public void SerializeByte()
        {
            MakePrimitiveTest<Byte>(0);
            MakePrimitiveTest<Byte>(-0);
            MakePrimitiveTest<Byte>(1);
            MakePrimitiveTest<Byte>(10);
            MakePrimitiveTest<Byte>(128);
            MakePrimitiveTest<Byte>(255);
        }

        [TestMethod]
        public void SerializeBytes()
        {
            var comparator = new Func<byte[], byte[], bool>((left, right) =>
                (left == null && (right == null || right.Length == 0)) || ArrayExtensions.UnsafeEquals(left, right));
            MakePrimitiveTest<Byte[]>(null, comparator);
            MakePrimitiveTest<Byte[]>(new byte[] { }, comparator);
            MakePrimitiveTest<Byte[]>(new byte[] { 1 }, comparator);
            MakePrimitiveTest<Byte[]>(new byte[] { 0, 1, 10, 100, 128, 255 }, comparator);
        }

        /*
         COLLECTIONS
         */

        [TestMethod]
        public void SerializeCollectionDateTime()
        {
            MakeCollectionTest<DateTime>(null);
            MakeCollectionTest<DateTime>(new DateTime[] { });
            MakeCollectionTest<DateTime>(new DateTime[] { DateTime.Now, DateTime.UtcNow, DateTime.Today, DateTime.Now.AddYears(2000), DateTime.MinValue, DateTime.MaxValue });
        }

        [TestMethod]
        public void SerializeCollectionIPAddress()
        {
            var comparator = new Func<IPAddress, IPAddress, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakeCollectionTest<IPAddress>(null);
            MakeCollectionTest<IPAddress>(new IPAddress[] { IPAddress.Any, IPAddress.Broadcast, IPAddress.IPv6Any, IPAddress.IPv6Loopback, IPAddress.IPv6None, IPAddress.Loopback, IPAddress.None, IPAddress.Parse("93.111.16.58") }, comparator);
        }

        [TestMethod]
        public void SerializeCollectionIPEndPoint()
        {
            var comparator = new Func<IPEndPoint, IPEndPoint, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakeCollectionTest<IPEndPoint>(null);
            MakeCollectionTest<IPEndPoint>(new IPEndPoint[] { });
            MakeCollectionTest<IPEndPoint>(new IPEndPoint[] { new IPEndPoint(IPAddress.Any, 1), new IPEndPoint(IPAddress.Broadcast, 600), new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MaxPort), new IPEndPoint(IPAddress.IPv6Loopback, 8080), new IPEndPoint(IPAddress.IPv6None, 80), new IPEndPoint(IPAddress.Loopback, 9000), new IPEndPoint(IPAddress.None, 0), new IPEndPoint(IPAddress.Parse("93.111.16.58"), IPEndPoint.MinPort) }, comparator);
        }

        [TestMethod]
        public void SerializeCollectionGuid()
        {
            MakeCollectionTest<Guid>(null);
            MakeCollectionTest<Guid>(new Guid[] { });
            MakeCollectionTest<Guid>(new Guid[] { Guid.Empty, Guid.NewGuid() });
        }

        [TestMethod]
        public void SerializeCollectionTimeSpan()
        {
            MakeCollectionTest<TimeSpan>(new TimeSpan[] { TimeSpan.MaxValue, TimeSpan.MinValue, TimeSpan.Zero, TimeSpan.FromDays(1024), TimeSpan.FromMilliseconds(1), TimeSpan.FromTicks(1), TimeSpan.FromTicks(0) });
        }

        [TestMethod]
        public void SerializeCollectionString()
        {
            var comparator = new Func<string, string, bool>((left, right) =>
                    (left == null && right == null) ||
                    (left == null && right != null && right.Length == 0) ||
                    (left != null && left.Length == 0 && right == null) ||
                    string.Compare(left, right, StringComparison.InvariantCulture) == 0);
            MakeCollectionTest<String>(new string[] { "", String.Empty, null, "HELLO!", "𐌼𐌰𐌲 𐌲𐌻𐌴𐍃 𐌹̈𐍄𐌰𐌽, 𐌽𐌹 𐌼𐌹𐍃 𐍅𐌿 𐌽𐌳𐌰𐌽 𐌱𐍂𐌹𐌲𐌲𐌹𐌸" }, comparator);
        }


        [TestMethod]
        public void SerializeCollectionInt32()
        {
            MakeCollectionTest<Int32>(new int[] { -0, 0, -10, 10, Int32.MinValue, Int32.MaxValue });
        }

        [TestMethod]
        public void SerializeCollectionInt64()
        {
            MakeCollectionTest<Int64>(new long[] { -0, 0, -10, 10, Int64.MinValue, Int64.MaxValue, Int64.MinValue / 2, Int64.MaxValue / 2 });
        }

        [TestMethod]
        public void SerializeCollectionDecimal()
        {
            MakeCollectionTest<Decimal>(new Decimal[] { -0, 0, -10, 10, Decimal.MinValue, Decimal.MaxValue, Decimal.MinValue / 2, Decimal.MaxValue / 2 });
        }

        [TestMethod]
        public void SerializeCollectionFloat()
        {
            MakeCollectionTest<float>(new float[] { -0, 0, -10, 10, float.MinValue, float.MaxValue, float.MinValue / 2, float.MaxValue / 2 });
        }

        [TestMethod]
        public void SerializeCollectionDouble()
        {
            MakeCollectionTest<Double>(new Double[] { -0, 0, -10, 10, Double.MinValue, Double.MaxValue, Double.MinValue / 2, Double.MaxValue / 2 });
        }

        [TestMethod]
        public void SerializeCollectionBoolean()
        {
            MakeCollectionTest<Boolean>(new Boolean[] { true, false, true });
        }

        [TestMethod]
        public void SerializeCollectionByte()
        {
            MakeCollectionTest<Byte>(new byte[] { 0, 3, -0, 1, 10, 128, 255 });
        }

        [TestMethod]
        public void SerializeCollectionBytes()
        {
            var comparator = new Func<byte[], byte[], bool>((left, right) =>
                (left == null && (right == null || right.Length == 0)) || ArrayExtensions.UnsafeEquals(left, right));

            MakeCollectionTest<Byte[]>(new Byte[][] { null, new byte[] { }, new byte[] { 1 }, new byte[] { 0, 1, 10, 100, 128, 255 } }, comparator);
        }

        [TestMethod]
        public void SerializeCompositeObject()
        {
            var comparator = new Func<Document, Document, bool>((left, right) =>
            {
                var l_bin = MessageSerializer.Serialize(left);
                var r_bin = MessageSerializer.Serialize(right);
                return ArrayExtensions.UnsafeEquals(l_bin, r_bin);
            });

            MakePrimitiveTest<Document>(MakeDocument(), comparator);
        }

        private static Document MakeDocument()
        {
            var doc = new Document();
            doc.Categories.Add(new Category
            {
                Title = "Algorithms",
                Code = "0",
                DirectionCode = "Posts"
            });
            doc.Categories.Add(new Category
            {
                Title = "Data compression",
                Code = "133",
                DirectionCode = "Posts"
            });
            doc.Header = "Парадоксы о сжатии данных";
            doc.Summary = "Задача сжатия данных в своей простейшей форме может относиться к числам и их обозначениям. Числа можно обозначать числительными («одиннадцать» для числа 11), математическими выражениями («два в двадцатой» для 1048576), строковыми выражениями («пять девяток» для 99999), именами собственными («число зверя» для 666, «год смерти Тьюринга» для 1954), или произвольными их комбинациями. Годится любое обозначение, по которому собеседник сможет однозначно определить, о каком числе речь. Очевидно, что сообщить собеседнику «факториал восьми» эффективнее, чем эквивалентное обозначение «сорок тысяч триста двадцать». Здесь возникает логичный вопрос: какое обозначение для заданного числа самое короткое?";
            doc.DescriptiveMetadata.Byline = "tyomitch";
            doc.DescriptiveMetadata.Source.Title = "HABR";

            doc.TagMetadata.Keywords.Add("парадокс берри");
            doc.TagMetadata.Keywords.Add("колмогоровская сложность");
            doc.TagMetadata.Keywords.Add("проблема останова");

            doc.Identifier.Link = "https://habr.com/en/post/446976/";

            var builder = new ContentBuilder(doc);
            builder.EnterParagraph();
            builder.WriteText("Философ Бертран Рассел в 1908 опубликовал");
            builder.WriteLink("https://ru.wikipedia.org/wiki/%D0%9F%D0%B0%D1%80%D0%B0%D0%B4%D0%BE%D0%BA%D1%81_%D0%91%D0%B5%D1%80%D1%80%D0%B8", "«парадокс Берри»");
            builder.WriteText(", который затрагивает вопрос обозначений чисел с противоположной стороны: какое самое маленькое число, для обозначения которого недостаточно восьмидесяти букв? Такое число обязано существовать: из восьмидесяти русских букв и пробелов можно составить всего 3480 обозначений, значит, с использованием восьмидесяти букв можно обозначить не более 3480 чисел.Значит, некое число, не большее чем 3480, обозначить таким образом невозможно.");
            builder.EnterParagraph();
            builder.WriteText("Значит, этому числу будет соответствовать обозначение «самое маленькое число, для обозначения которого недостаточно восьмидесяти букв», в котором всего 78 букв! С одной стороны, это число обязано существовать; с другой, если это число существует, то его обозначение ему не соответствует. Парадокс!");
            builder.EnterParagraph();
            builder.WriteText("Самый простой способ отмахнуться от этого парадокса — сослаться на неформальность словесных обозначений. Мол, если бы в обозначениях допускался лишь конкретно определённый набор выражений, то «самое маленькое число, для обозначения которого недостаточно восьмидесяти букв» не было бы допустимым обозначением, тогда как практически полезные обозначения типа «факториал восьми» остались бы допустимыми.");
            builder.EnterParagraph();
            builder.WriteText("Есть ли формальные способы описания последовательности (алгоритма) действий над числами? Есть, и в изобилии — их называют языками программирования. Будем вместо словесных обозначений использовать программы (например, на Python), выводящие нужные числа. Например, для пяти девяток подойдёт программа print(\"9\"*5). По-прежнему будем интересоваться самой короткой программой для заданного числа. Длину такой программы называют колмогоровской сложностью числа; это теоретический предел, до которого заданное число можно сжать.");
            builder.EnterParagraph();
            builder.WriteText("Вместо парадокса Берри теперь можно рассмотреть аналогичный: какое самое маленькое число, для вывода которого недостаточно килобайтной программы?");
            builder.EnterParagraph();
            builder.WriteText("Рассуждать будем так же, как и раньше: существует 2561024 килобайтных текстов, значит, килобайтными программами можно вывести не более 2561024 чисел. Значит, некое число, не большее чем 2561024, вывести таким способом невозможно.");
            builder.EnterParagraph();
            builder.WriteText("Но напишем на Python программу, которая генерирует все возможные килобайтные тексты, запускает их на выполнение, и если они выводят какое-то число — то добавляет это число в словарь достижимых. После проверки всех 2561024 возможностей, сколько бы времени это ни заняло — программа ищет, какое самое маленькое число отсутствует в словаре, и выводит это число. Кажется очевидным, что такая программа уложится в килобайт кода — и выведет то самое число, которое невозможно вывести килобайтной программой!");
            builder.EnterParagraph();
            builder.WriteText("В чём же подвох теперь? На неформальность обозначений его списать уже нельзя!");
            builder.EnterParagraph();
            builder.WriteText("Если вас смущает то, что наша программа потребует астрономического количества памяти для работы — словарь (или битовый массив) из 2561024 элементов — то можно всё то же самое осуществить и без него: для каждого из 2561024 чисел по очереди перебирать все 2561024 возможных программ, пока не найдётся подходящая. Не важно, что такой перебор продлится очень долго: после проверки менее чем (2561024)2 пар из числа и программы он ведь завершится, и найдёт то самое заветное число.");
            builder.EnterParagraph();
            builder.WriteText("Или не завершится? Ведь среди всех программ, которые будут испробованы, встретится while True: pass (и её функциональные аналоги) — и дальше проверки такой программы дело уже не пойдёт!");
            builder.EnterParagraph();
            builder.WriteText("В отличие от парадокса Берри, где подвох был в неформальности обозначений, во втором случае мы имеем хорошо замаскированную переформулировку «проблемы остановки». Дело в том, что по программе невозможно за конечное время определить её вывод.В частности, колмогоровская сложность невычислима: нет никакого алгоритма, который бы позволил для заданного числа найти длину самой короткой программы, выводящей это число; а значит, нет решения и для задачи Берри — найти для заданного числа длину самого короткого словесного обозначения.");
            builder.Complete();

            var c1 = new Comment
            {
                Id = 0,
                RefToId = -1,
                Author = "exception13x",
                Text = "С каких пор пробелы и запятые стали буквами? Если использовать «символов», то парадокс пропадает.",
                Votes = 0,
                DateTime = DateTime.Parse("2019-04-09 10:33")
            };
            var c2 = new Comment
            {
                Id = 1,
                RefToId = 0,
                Author = "tyomitch",
                Text = "Почему пропадает?",
                Votes = 1,
                DateTime = DateTime.Parse("2019-04-09 10:57")
            };
            var c3 = new Comment
            {
                Id = 2,
                RefToId = 1,
                Author = "exception13x",
                Text = "Потому что в фразе «самое маленькое число, для обозначения которого недостаточно восьмидесяти символов» будет 83 символа. Прошу прощения за придирки, меня просто зацепила фраза про 78 букв(которых 69 в исходной фразе)",
                Votes = -1,
                DateTime = DateTime.Parse("2019-04-09 12:12")
            };
            var c4 = new Comment
            {
                Id = 3,
                RefToId = 2,
                Author = "kahi4",
                Text = "ну поменять 80 на 100 и делов то, \"самое маленькое число, для обозначения которого недостаточно ста символов\".",
                Votes = -1,
                DateTime = DateTime.Parse("2019-04-09 12:50")
            };
            doc.Attachments.Add(new AttachContent("Comment", ContentType.Raw).Write(c1));
            doc.Attachments.Add(new AttachContent("Comment", ContentType.Raw).Write(c2));
            doc.Attachments.Add(new AttachContent("Comment", ContentType.Raw).Write(c3));
            doc.Attachments.Add(new AttachContent("Comment", ContentType.Raw).Write(c4));
            return doc;
        }

        private class Comment :
            IBinarySerializable, IEquatable<Comment>
        {
            public long Id;
            public long RefToId;

            public DateTime DateTime;
            public string Author;

            public string Text;
            public int Votes;

            public void Deserialize(IBinaryReader reader)
            {
                this.Id = reader.ReadLong();
                this.RefToId = reader.ReadLong();
                this.DateTime = reader.ReadDateTime().Value;
                this.Author = reader.ReadString();
                this.Text = reader.ReadString();
                this.Votes = reader.ReadInt32();
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as Comment);
            }

            public override int GetHashCode()
            {
                return this.Id.GetHashCode();
            }

            public bool Equals(Comment other)
            {
                if (other == null) return false;
                return this.Id == other.Id && this.RefToId == other.RefToId && this.Votes == other.Votes;
            }

            public void Serialize(IBinaryWriter writer)
            {
                writer.WriteLong(this.Id);
                writer.WriteLong(this.RefToId);
                writer.WriteDateTime(this.DateTime);
                writer.WriteString(this.Author);
                writer.WriteString(this.Text);
                writer.WriteInt32(this.Votes);
            }
        }
    }
}

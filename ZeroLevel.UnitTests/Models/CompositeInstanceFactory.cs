using DOM.Services;
using System;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.UnitTests.Models
{
    public static class CompositeInstanceFactory
    {
        public static Document MakeDocument()
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

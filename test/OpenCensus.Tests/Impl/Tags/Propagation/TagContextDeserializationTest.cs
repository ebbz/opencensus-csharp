﻿using OpenCensus.Internal;
using OpenCensus.Tags.Propagation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OpenCensus.Tags.Propagation.Test
{
    public class TagContextDeserializationTest
    {
        private readonly TagsComponent tagsComponent = new TagsComponent();
        private readonly ITagContextBinarySerializer serializer;
        private readonly ITagger tagger;

        public TagContextDeserializationTest()
        {
            serializer = tagsComponent.TagPropagationComponent.BinarySerializer;
            tagger = tagsComponent.Tagger;
        }
        [Fact]
        public void TestConstants()
        {
            // Refer to the JavaDoc on SerializationUtils for the definitions on these constants.
            Assert.Equal(0, SerializationUtils.VERSION_ID);
            Assert.Equal(0, SerializationUtils.TAG_FIELD_ID);
            Assert.Equal(8192, SerializationUtils.TAGCONTEXT_SERIALIZED_SIZE_LIMIT);
        }

        [Fact]
        public void TestDeserializeNoTags()
        {
            ITagContext expected = tagger.Empty;
            ITagContext actual = serializer.FromByteArray(new byte[] { SerializationUtils.VERSION_ID }); // One byte that represents Version ID.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDeserializeEmptyByteArrayThrowException()
        {
            Assert.Throws<TagContextDeserializationException>(() => serializer.FromByteArray(new byte[0]));
        }

        [Fact]
        public void TestDeserializeTooLargeByteArrayThrowException()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);
            for (int i = 0; i < SerializationUtils.TAGCONTEXT_SERIALIZED_SIZE_LIMIT / 8 - 1; i++) {
                // Each tag will be with format {key : "0123", value : "0123"}, so the length of it is 8.
                String str;
                if (i < 10)
                {
                    str = "000" + i;
                }
                else if (i < 100)
                {
                    str = "00" + i;
                }
                else if (i < 1000)
                {
                    str = "0" + i;
                }
                else
                {
                    str = i.ToString();
                }
                EncodeTagToOutPut(str, str, output);
            }
            // The last tag will be of size 9, so the total size of the TagContext (8193) will be one byte
            // more than limit.
            EncodeTagToOutPut("last", "last1", output);

            byte[] bytes = output.ToArray();

            Assert.Throws<TagContextDeserializationException>(() => serializer.FromByteArray(bytes));
        }

        // Deserializing this inPut should cause an error, even though it represents a relatively small
        // TagContext.
        [Fact]
        public void TestDeserializeTooLargeByteArrayThrowException_WithDuplicateTagKeys()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);
            for (int i = 0; i < SerializationUtils.TAGCONTEXT_SERIALIZED_SIZE_LIMIT / 8 - 1; i++) {
                // Each tag will be with format {key : "key_", value : "0123"}, so the length of it is 8.
                String str;
                if (i < 10)
                {
                    str = "000" + i;
                }
                else if (i < 100)
                {
                    str = "00" + i;
                }
                else if (i < 1000)
                {
                    str = "0" + i;
                }
                else
                {
                    str = i.ToString();
                }
                EncodeTagToOutPut("key_", str, output);
            }
            // The last tag will be of size 9, so the total size of the TagContext (8193) will be one byte
            // more than limit.
            EncodeTagToOutPut("key_", "last1", output);

            byte[] bytes = output.ToArray();

            Assert.Throws<TagContextDeserializationException>(() => serializer.FromByteArray(bytes));
        }

        [Fact]
        public void TestDeserializeInvalidTagKey()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);

            // Encode an invalid tag key and a valid tag value:
            EncodeTagToOutPut("\u0002key", "value", output);
            byte[] bytes = output.ToArray();


            Assert.Throws<TagContextDeserializationException>(() => serializer.FromByteArray(bytes));
        }

        [Fact]
        public void TestDeserializeInvalidTagValue()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);

            // Encode a valid tag key and an invalid tag value:
            EncodeTagToOutPut("my key", "val\u0003", output);
            byte[] bytes = output.ToArray();


            Assert.Throws<TagContextDeserializationException>(() => serializer.FromByteArray(bytes));
        }

        [Fact]
        public void TestDeserializeOneTag()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);
            EncodeTagToOutPut("Key", "Value", output);
            ITagContext expected = tagger.EmptyBuilder.Put(TagKey.Create("Key"), TagValue.Create("Value")).Build();
            Assert.Equal(expected, serializer.FromByteArray(output.ToArray()));
        }

        [Fact]
        public void TestDeserializeMultipleTags()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);
            EncodeTagToOutPut("Key1", "Value1", output);
            EncodeTagToOutPut("Key2", "Value2", output);
            ITagContext expected =
                    tagger
                    .EmptyBuilder
                    .Put(TagKey.Create("Key1"), TagValue.Create("Value1"))
                    .Put(TagKey.Create("Key2"), TagValue.Create("Value2"))
                    .Build();
            Assert.Equal(expected, serializer.FromByteArray(output.ToArray()));
        }

        [Fact]
        public void TestDeserializeDuplicateKeys()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);
            EncodeTagToOutPut("Key1", "Value1", output);
            EncodeTagToOutPut("Key1", "Value2", output);
            ITagContext expected =
                tagger.EmptyBuilder.Put(TagKey.Create("Key1"), TagValue.Create("Value2")).Build();

            Assert.Equal(expected, serializer.FromByteArray(output.ToArray()));
        }

        [Fact]
        public void TestDeserializeNonConsecutiveDuplicateKeys()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);
            EncodeTagToOutPut("Key1", "Value1", output);
            EncodeTagToOutPut("Key2", "Value2", output);
            EncodeTagToOutPut("Key3", "Value3", output);
            EncodeTagToOutPut("Key1", "Value4", output);
            EncodeTagToOutPut("Key2", "Value5", output);
            ITagContext expected =
                tagger
                    .EmptyBuilder
                    .Put(TagKey.Create("Key1"), TagValue.Create("Value4"))
                    .Put(TagKey.Create("Key2"), TagValue.Create("Value5"))
                    .Put(TagKey.Create("Key3"), TagValue.Create("Value3"))
                    .Build();
            Assert.Equal(expected, serializer.FromByteArray(output.ToArray()));
        }

        [Fact]
        public void TestDeserializeDuplicateTags()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);
            EncodeTagToOutPut("Key1", "Value1", output);
            EncodeTagToOutPut("Key1", "Value1", output);
            ITagContext expected =
                tagger.EmptyBuilder.Put(TagKey.Create("Key1"), TagValue.Create("Value1")).Build();
            Assert.Equal(expected, serializer.FromByteArray(output.ToArray()));
        }

        [Fact]
        public void TestDeserializeNonConsecutiveDuplicateTags()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);
            EncodeTagToOutPut("Key1", "Value1", output);
            EncodeTagToOutPut("Key2", "Value2", output);
            EncodeTagToOutPut("Key3", "Value3", output);
            EncodeTagToOutPut("Key1", "Value1", output);
            EncodeTagToOutPut("Key2", "Value2", output);
            ITagContext expected =
                tagger
                    .EmptyBuilder
                    .Put(TagKey.Create("Key1"), TagValue.Create("Value1"))
                    .Put(TagKey.Create("Key2"), TagValue.Create("Value2"))
                    .Put(TagKey.Create("Key3"), TagValue.Create("Value3"))
                    .Build();
            Assert.Equal(expected, serializer.FromByteArray(output.ToArray()));
        }

        [Fact]
        public void StopParsingAtUnknownField()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);
            EncodeTagToOutPut("Key1", "Value1", output);
            EncodeTagToOutPut("Key2", "Value2", output);

            // Write unknown field ID 1.
            output.WriteByte(1);
            output.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);

            EncodeTagToOutPut("Key3", "Value3", output);

            // key 3 should not be included
            ITagContext expected =
                tagger
                    .EmptyBuilder
                    .Put(TagKey.Create("Key1"), TagValue.Create("Value1"))
                    .Put(TagKey.Create("Key2"), TagValue.Create("Value2"))
                    .Build();
            Assert.Equal(expected, serializer.FromByteArray(output.ToArray()));
        }

        [Fact]
        public void StopParsingAtUnknownTagAtStart()
        {
            MemoryStream output = new MemoryStream();
            output.WriteByte(SerializationUtils.VERSION_ID);

            // Write unknown field ID 1.
            output.WriteByte(1);
            output.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);

            EncodeTagToOutPut("Key", "Value", output);
            Assert.Equal(tagger.Empty, serializer.FromByteArray(output.ToArray()));
        }

        [Fact]
        public void TestDeserializeWrongFormat()
        {
            // encoded tags should follow the format <version_id>(<tag_field_id><tag_encoding>)*
            Assert.Throws<TagContextDeserializationException>(() => serializer.FromByteArray(new byte[3]));
        }

        [Fact]
        public void TestDeserializeWrongVersionId()
        {

            Assert.Throws<TagContextDeserializationException>(() => serializer.FromByteArray(new byte[] { (byte)(SerializationUtils.VERSION_ID + 1) }));
        }

        [Fact]
        public void TestDeserializeNegativeVersionId()
        {
            Assert.Throws<TagContextDeserializationException>(() => serializer.FromByteArray(new byte[] { (byte)0xff }));
        }

        //     <tag_encoding> ==
        //       <tag_key_len><tag_key><tag_val_len><tag_val>
        //         <tag_key_len> == varint encoded integer
        //         <tag_key> == tag_key_len bytes comprising tag key name
        //         <tag_val_len> == varint encoded integer
        //         <tag_val> == tag_val_len bytes comprising UTF-8 string
        private static void EncodeTagToOutPut(String key, String value, MemoryStream output)
        {
            output.WriteByte(SerializationUtils.TAG_FIELD_ID);
            EncodeString(key, output);
            EncodeString(value, output);
        }

        private static void EncodeString(String input, MemoryStream output)
        {
            int length = input.Length;
            byte[] bytes = new byte[VarInt.VarIntSize(length)];
            VarInt.PutVarInt(length, bytes, 0);
            output.Write(bytes, 0, bytes.Length);
            byte[] inPutBytes = Encoding.UTF8.GetBytes(input);
            output.Write(inPutBytes, 0, inPutBytes.Length);
        }
    }
}

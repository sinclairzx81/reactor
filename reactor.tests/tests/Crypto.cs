using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Reactor.Tests
{
    public static class Crypto
    {
        public static void TestRijndael()
        {
            //-------------------------------------------
            // setup encryptor
            //-------------------------------------------
            var rijndael = Rijndael.Create();

            var encryptor = Reactor.Crypto.Transform.Create( rijndael.CreateEncryptor(rijndael.Key, rijndael.IV) );

            encryptor.OnData += (data) => {

                Console.WriteLine( data.ToString(Encoding.UTF8) );
            };

            encryptor.OnEnd += () => {

                Console.WriteLine("ended");
            };

            //-------------------------------------------
            // encrypt data
            //-------------------------------------------

            string message = "this is a string to encrypt";

            encryptor.Write(message);

            encryptor.End();
        }

        public static void TestRijndaelFile()
        {
            var rijndael = Rijndael.Create();

            var input     = Reactor.File.ReadStream.Create("c:/input/cat.jpg");

            var encryptor = Reactor.Crypto.Transform.Create( rijndael.CreateEncryptor(rijndael.Key, rijndael.IV));

            var encrypted = Reactor.File.WriteStream.Create("c:/input/encrypted.jpg");

            input.Pipe(encryptor).Pipe(encrypted);

            input.OnEnd += () => {

                input         = Reactor.File.ReadStream.Create("c:/input/encrypted.jpg");

                var decryptor = Reactor.Crypto.Transform.Create(rijndael.CreateDecryptor(rijndael.Key, rijndael.IV));

                var decrypted = Reactor.File.WriteStream.Create("c:/input/decrypted2.jpg");

                input.Pipe(decryptor).Pipe(decrypted);
            };        
        }
    }
}

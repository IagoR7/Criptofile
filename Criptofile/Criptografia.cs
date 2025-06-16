using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace Criptofile
{
    internal class Criptografia
    {
        // Declaração de CSpParmeters e RsaCryptoServiceProvider
        // Objetos com escopo global na classe.
        public static CspParameters cspp;
        public static RSACryptoServiceProvider rsa;


        /* caminhos variáveis para a fonte, pasta de criptografia,
         e pasta de descriptografia */

        private static string _encrFolder;

        public static string EncrFolder
        {
            get
            {
                return _encrFolder;
            }
            set
            {
                _encrFolder = value;
                // Definir o caminho
                PubKeyFile = _encrFolder + "rsaPublicKey.txt";
            }
        }

        public static string DecrFolder { get; set; }
        public static string SrcFolder { get; set; }

        // Arquivo de Chave Publica

        private static string PubKeyFile = EncrFolder + "rsaPublicKey.txt";

        // Chave contendo o nome para private/public key value pair.
        public static string keyName;
        // Metodo para criar a chave publica
        public static string CreateAsmKeys()
        {
            string result = "";

            // Armazena uma key pair na key container
            if (string.IsNullOrEmpty(keyName))
            {
                result = "Chave Publica não definida";
                return result;
            }
            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;
            if (rsa.PublicOnly)
            {
                result = "Key : " + cspp.KeyContainerName + " - Somente Publica";
            }
            else
            {
                result = "Key : " + cspp.KeyContainerName + " - Key Pair Completa";
            }

                return result;
        }

        // método para exportar a chave publica a um arquivo
        public static bool ExportPublicKey()
        {
            bool result = true;

            if (rsa == null)
            {
                return false;
            }

            if (!Directory.Exists(EncrFolder))
            {
                Directory.CreateDirectory(EncrFolder);
            }

            StreamWriter sw = new StreamWriter(PubKeyFile, false);
            try
            {
                sw.Write(rsa.ToXmlString(false));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                result = false;
                
            }
            finally
            {
                sw.Close();
            }


            return result;
        }

        // metodo para importar a chave publica de um arquivo

        public static string ImportPublicKey()
        {
            string result = "";
            if (!File.Exists(PubKeyFile))
            {
                result = "Arquivo de chave public não encontrado";
                return result;
            }


            if (string.IsNullOrEmpty(keyName))
            {
                result = "Chave publica não definida";
                return result;
            }

            StreamReader sr = new StreamReader(PubKeyFile);

            try
            {
                cspp.KeyContainerName = keyName;
                rsa = new RSACryptoServiceProvider(cspp);
                string keytxt = sr.ReadToEnd();
                rsa.FromXmlString(keytxt);
                rsa.PersistKeyInCsp = true;
                if (rsa.PublicOnly)
                {
                    result = "Key: " + cspp.KeyContainerName + " - Somente Publica";
                }
                else
                {
                    result = "Key: " + cspp.KeyContainerName + " - Key Pair Completa";
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
                Console.WriteLine(ex.Message);
                
            }
            finally
            {
                sr.Close();
            }

            return result; 
        }

        // Metodo para criar uma chave privada á partir de um valor definido
        public static string GetPrivateKey()
        {
            string result = "";

            if (string.IsNullOrEmpty(keyName))
            {
                result = "Chave privade não definida";
                return result;
                  
            }
            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;
            if (rsa.PublicOnly)
            {
                result = "Key: " + cspp.KeyContainerName + " - Somente Publica";
            }
            else
            {
                result = "Key: " + cspp.KeyContainerName + " - Key Pair Completa";
            }
            return result;
        }

        // Metodo para criptografar um arquivo
        public static string EncryptFile(string inFile)
        {
            // criar uma instância de Aes para criptografia simétrica dos dados
            Aes aes = Aes.Create();
            ICryptoTransform transform = aes.CreateEncryptor();

            /* Use RSACryptoServiceProvider para criptografar a chave AES.
             rsa é instanciado anteriormente: rsa = new RSACryptoServiceProvider(cspp);*/

            byte[] keyEncrypted = rsa.Encrypt(aes.Key, false);

            /* Crie matrizes de bytes para conter os valores de comprimente da chave e IV*/
            
            byte[] LenK = new byte[4];
            
            byte[] LenIV = new byte[4];

            int lKey = keyEncrypted.Length;
            LenK = BitConverter.GetBytes(lKey);

            int lIV = aes.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);

            // - Escreva o seguinto no FileStream
            // - Para o arquivo criptografado(outFs):
            // - Comprimento da chave
            // - chave criptografada
            // - comprimento do IV
            // - o IV
            // - o Conteúdo da cifra criptografa

            int startFileName = inFile.LastIndexOf("\\") + 1;
            string outFile = EncrFolder + inFile.Substring(startFileName) + ".enc";

            try
            {
                using (FileStream outFs = new FileStream(outFile, FileMode.Create))
                {
                    outFs.Write(LenK, 0, 4);
                    outFs.Write(LenIV, 0, 4);
                    outFs.Write(keyEncrypted, 0, lKey);
                    outFs.Write(aes.IV, 0, lIV);

                    // Escrevendo o texto cifrado usando um CryptoStream para criptografar.
                    using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                    {
                        // Ao criptografar um pedaçõ por vez, voce pode econimizar memória.

                        int count = 0;
                        int offset = 0;

                        // blockSizeBytes pode ter qualquer tamanho arbitrário.
                        int blockSizeBytes = aes.BlockSize / 8;
                        byte[] data = new byte[blockSizeBytes];
                        int bytesRead = 0;

                        using (FileStream inFs = new FileStream(inFile, FileMode.Open))
                        {
                            do
                            {
                                count = inFs.Read(data, 0, blockSizeBytes);
                                offset += count;
                                outStreamEncrypted.Write(data, 0, count);
                                bytesRead += blockSizeBytes;
                            } while (count > 0);
                            inFs.Close();
                        }
                        outStreamEncrypted.FlushFinalBlock();
                        outStreamEncrypted.Close();
                    }
                    outFs.Close();
                    File.Delete(inFile);
                }

            }
            catch (Exception ex)
            {

                return ex.Message;
            }
            return $"Arquivo criptografado.\n" + $"Origem: {inFile}\n" + $"Destino: {outFile}";
        }

        // metodo para descriptografar um arquivo
        public static string DecryptFile(string inFile)
        {
            // crira instância de Aes para Descriptografia simétrica dos dados.
            Aes aes = Aes.Create();

            // Criar matrizes de bytes para obter o comprimento de cada chave criptografa e IV
            // Esses Valores foram armazenados com 4 bytes cada no início do pacota criptografado.

            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            // construir o nome do arquivo para o arquivo descriptografado

            string outFile = DecrFolder + inFile.Substring(0, inFile.LastIndexOf("."));

            try
            {
                // Use objetos FileStream para ler o criptografado (inFs) e salve o arquivo descriptografado(outFs).
                using (FileStream inFs = new FileStream(EncrFolder + inFile, FileMode.Open))
                {
                    inFs.Seek(0, SeekOrigin.Begin);
                    inFs.Seek(0, SeekOrigin.Begin);
                    inFs.Read(LenK, 0, 3);
                    inFs.Seek(4, SeekOrigin.Begin);
                    inFs.Read(LenIV, 0, 3);

                    // Converter os comprimentos em valores inteiros.

                    int lenK = BitConverter.ToInt32(LenK, 0);
                    int lenIV = BitConverter.ToInt32(LenIV, 0);

                    // Determine a posição inicial do texto cifrado(startC) e seu comprimento(lenS).
                    int startC = lenK + lenIV + 8;
                    int lenC = (int)inFs.Length - startC;

                    // Criar as matrizes de bytes para a chave Aes criptografada, o IV e o texto cifrado.

                    byte[] KeyEncrypted = new byte[lenK];
                    byte[] IV = new byte[lenIV];

                    // Extrair a chave e IV começando do indice 8 apos os valores de comprimento
                    inFs.Seek(8, SeekOrigin.Begin);
                    inFs.Read(KeyEncrypted, 0, lenK);
                    inFs.Seek(8 + lenK, SeekOrigin.Begin);

                    if (!Directory.Exists(DecrFolder))
                    {
                        Directory.CreateDirectory(DecrFolder);
                    }

                    // Use RSACryptoServiceProvider para descriptografar a chave AES.
                    byte[] KeyDecrypted = rsa.Decrypt(KeyEncrypted, false);

                    // Descriptografe a chave.

                    ICryptoTransform transform = aes.CreateDecryptor(KeyDecrypted, IV);

                    // Descriptografar o texto cifrado do FileStream do arquivo(inFS)
                    // criptografado no FileStream para o arquivo descriptografado(outFs).

                    using (FileStream outFs = new FileStream(outFile, FileMode.Create))
                    {
                        int count = 0;
                        int offset = 0;

                        // blockSizeBytes pode ter qualquer tamanho arbitrário.
                        int blockSizeBytes = aes.BlockSize / 8;
                        byte[] data = new byte[blockSizeBytes];
                        int bytesRead = 0;

                        // comece no início do texto cifrado
                        inFs.Seek(startC, SeekOrigin.Begin);

                        using (CryptoStream outStreamDecrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                        {
                            do
                            {
                                count = inFs.Read(data, 0, blockSizeBytes);
                                offset += count;
                                outStreamDecrypted.Write(data, 0, count);
                                bytesRead += blockSizeBytes;
                            } while (count > 0);

                            outStreamDecrypted.FlushFinalBlock();
                            outStreamDecrypted.Close();
                        }
                        outFs.Close();
                    }
                    inFs.Close();

                }
            }
            catch (Exception ex)
            {

                return ex.Message;
            }

            return $"Arquivo descriptografado.\n" + $"Origem: {inFile}\n" + $"Destino: {outFile}";
        }
    }
}

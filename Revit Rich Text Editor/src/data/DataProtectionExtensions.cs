using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace CTEK_Rich_Text_Editor
{
    public static class DataProtectionExtensions
    {
        // This can be changed whenever. It'll just invalidate the previously saved password.
        private static byte[] entropyBytes = Encoding.Unicode.GetBytes("=Nk#DFgYNUF!63WbAWbys+8N3PcYvWd2M@URMeuBH*@A5KdzGM@=yFve?CS$DBGupTyFb4@j7eqUf9xJBMfme7Mw9YHMw8qRKdFSyan@5$!P-UvuAzWhY3gjxF42BLKH*xdY&=c%_H!44GTc5XGg46-v!g=NQf5K%&Z3?K+-M-#5_nwCjc#24SNJnjmZy8CKg3wKUuSwJ+-whf2_PnfF2eX_*d9BQ%vaTTtTGGJDskbsELDAeJ-z%#V*k*p5^5Ac");

        public static string EncryptPassword(string password)
        {
            byte[] plainBytes = Encoding.Unicode.GetBytes(password);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, entropyBytes, DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(encryptedBytes);
        }

        // Decrypts the pass or returns null if the decryption failed
        public static string DecryptPassword(string password)
        {
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(password);
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, entropyBytes, DataProtectionScope.CurrentUser);

                return Encoding.Unicode.GetString(decryptedBytes);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

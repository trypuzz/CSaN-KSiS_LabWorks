namespace tracert;

public class ICMP
{
    public ICMP(byte[] package)
    {
        package[0] = 8;  
        package[1] = 0;  
        
        package[2] = 0;  //CheckSum
        package[3] = 0;
        
        package[4] = 0;  //ID
        package[5] = 1;
        
        package[6] = 0;  //SeqNum
        package[7] = 1;
    }
    
    public void SequenceNumber(byte[] package, int number)
    {
        package[6] = (byte)(number >> 8);
        package[7] = (byte)(number);
    }
    
    public void CheckSum(byte[] package)
    {
        uint CheckSum = ((uint)package[0] << 8) + ((uint)package[1]);
        uint tmp = 0;
        for (int i = 4; i < package.Length; i += 2)
        {
            tmp = (uint)(package[i] << 8);
            tmp += (uint)package[i + 1];
            CheckSum += tmp;
        }
        CheckSum = (uint)(~CheckSum);
        package[2] = (byte)(CheckSum >> 8);
        package[3] = (byte)(CheckSum);
    }
    
}
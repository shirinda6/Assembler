using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    public class Assembler
    {
        private const int WORD_SIZE = 16;
      //  string[] num= {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private Dictionary<string, int[]> m_dControl, m_dJmp,m_dest; //these dictionaries map command mnemonics to machine code - they are initialized at the bottom of the class
        private Dictionary<string, int> symbols,labels;
        private int rowindex=0;
        private int numoflabel;
        private string Dcode="";
        private string control;
        private string dest;
        private string jump;


        //more data structures here (symbol map, ...)

        public Assembler()
        {
            InitCommandDictionaries();
        }

        //this method is called from the outside to run the assembler translation
        public void TranslateAssemblyFile(string sInputAssemblyFile, string sOutputMachineCodeFile)
        {
            //read the raw input, including comments, errors, ...
            StreamReader sr = new StreamReader(sInputAssemblyFile);
            List<string> lLines = new List<string>();
            while (!sr.EndOfStream)
            {
                lLines.Add(sr.ReadLine());
            }
            sr.Close();
            //translate to machine code
            List<string> lTranslated = TranslateAssemblyFile(lLines);
            //write the output to the machine code file
            StreamWriter sw = new StreamWriter(sOutputMachineCodeFile);
            foreach (string sLine in lTranslated)
                sw.WriteLine(sLine);
            sw.Close();
        }

        //translate assembly into machine code
        private List<string> TranslateAssemblyFile(List<string> lLines)
        {
            //implementation order:
            //first, implement "TranslateAssemblyToMachineCode", and check if the examples "Add", "MaxL" are translated correctly.
            //next, implement "CreateSymbolTable", and modify the method "TranslateAssemblyToMachineCode" so it will support symbols (translating symbols to numbers). check this on the examples that don't contain macros
            //the last thing you need to do, is to implement "ExpendMacro", and test it on the example: "SquareMacro.asm".
            //init data structures here 

            //expand the macros
            List<string> lAfterMacroExpansion = ExpendMacros(lLines);

            //first pass - create symbol table and remove lable lines
            CreateSymbolTable(lAfterMacroExpansion);

            //second pass - replace symbols with numbers, and translate to machine code
            List<string> lAfterTranslation = TranslateAssemblyToMachineCode(lAfterMacroExpansion);
            return lAfterTranslation;
        }

        
        //first pass - replace all macros with real assembly
        private List<string> ExpendMacros(List<string> lLines)
        {
            //You do not need to change this function, you only need to implement the "ExapndMacro" method (that gets a single line == string)
            List<string> lAfterExpansion = new List<string>();
            for (int i = 0; i < lLines.Count; i++)
            {
                //remove all redudant characters
                string sLine = CleanWhiteSpacesAndComments(lLines[i]);
                if (sLine == "")
                    continue;
                //if the line contains a macro, expand it, otherwise the line remains the same
                List<string> lExpanded = ExapndMacro(sLine);
                //we may get multiple lines from a macro expansion
                foreach (string sExpanded in lExpanded)
                {
                    lAfterExpansion.Add(sExpanded);
                }
            }
            return lAfterExpansion;
        }

        //expand a single macro line
        private List<string> ExapndMacro(string sLine)
        {
            List<string> lExpanded = new List<string>();

            if (IsCCommand(sLine))
            {
                string sDest, sCompute, sJmp;
                GetCommandParts(sLine, out sDest, out sCompute, out sJmp);
                if (sCompute.Contains("A+A")|| sCompute.Contains("D+D")|| sCompute.Contains("M+M")|| sCompute.Contains("A+M")||sCompute.Contains("M+A")|| sCompute.Contains("A-A")|| sCompute.Contains("A-M")|| sCompute.Contains("M-M") ||sCompute.Contains("D-D"))
                {
                    throw new FormatException("incorrect details");
                }
                //source;jump:<label>
                else if (sLine.Contains(":")) {
                    string s1 = sJmp.Substring(sJmp.IndexOf(":")+1);
                    string s2 = sJmp.Substring(0, sJmp.IndexOf(":"));
                     lExpanded.Add("@" + s1);
                     lExpanded.Add(sCompute + ";" + s2);
                }
                //++
                else if (sLine.Contains("++")) {
                    if (sCompute[0] != 'D' && sCompute[0] != 'A'&& sCompute[0] != 'M')
                    {
                        lExpanded.Add("@" + sCompute.Substring(0, sCompute.Length-2));
                        lExpanded.Add("M=M+1");
                    }
                    else
                        lExpanded.Add(sCompute.Substring(0, sCompute.Length - 2) + "=" + sCompute.Substring(0, sCompute.Length - 2) + "+" + "1" );
                }
                //--
                else if (sLine.Contains("--"))
                {
                    if (sCompute[0] != 'D' && sCompute[0] != 'A' && sCompute[0] != 'M')
                    {
                        lExpanded.Add("@" + sCompute.Substring(0, sCompute.Length - 2));
                        lExpanded.Add("M=M-1");
                    }
                    else
                        lExpanded.Add(sCompute.Substring(0, sCompute.Length - 2) + "=" + sCompute.Substring(0, sCompute.Length - 2) + "-" +"1");
                }
             
                else if (sLine.Contains("=")) {
                    if (!sLine.Contains("+") && !sLine.Contains("-"))
                    {
                        //Dest = <label>                                                      
                        if (sLine.Substring(0, sLine.IndexOf("=")) == "D" && sLine.Substring(sLine.IndexOf("=") + 1) != "M" && sLine.Substring(sLine.IndexOf("=") + 1) != "A")
                        {
                            lExpanded.Add("@" + sCompute);
                            lExpanded.Add("D=M");
                        }
                        //<label>= source
                        else if (sLine.Substring(0, sLine.IndexOf("=")) != "M" && sLine.Substring(0, sLine.IndexOf("=")) != "A" && sLine.Substring(sLine.IndexOf("=") + 1) == "D")
                        {
                            lExpanded.Add("@" + sDest);
                            lExpanded.Add("M=D");
                        }
                        else if (sLine.Substring(0, sLine.IndexOf("=")) == "A" && sLine.Substring(sLine.IndexOf("=")) != "A" && sLine.Substring(sLine.IndexOf("=") + 1) != "D" && sLine.Substring(sLine.IndexOf("=") + 1) != "M")
                        {
                            lExpanded.Add("@" + sCompute);
                            lExpanded.Add("A=M");
                        }

                        else if (!sCompute.Contains("A") && !sCompute.Contains("D") && !sCompute.Contains("M") && !sDest.Contains("A") && !sDest.Contains("D") && !sDest.Contains("M"))
                        {
                            lExpanded.Add("@" + sCompute);
                            lExpanded.Add("D=M");
                            lExpanded.Add("@" + sDest);
                            lExpanded.Add("M=D");
                        }
                    }                   
                }                
                //your code here - check for indirect addessing and for jmp shortcuts
                //read the word file to see all the macros you need to support
            }
            if (lExpanded.Count == 0)
                lExpanded.Add(sLine);
            return lExpanded;
        }

        //second pass - record all symbols - labels and variables
        private void CreateSymbolTable(List<string> lLines)
        {
            symbols = new Dictionary<string, int>();
            labels = new Dictionary<string, int>();

            for (int i=0; i<16; i++)
            {
                symbols["R" + i.ToString()] = i;
            }
            symbols["SCREEN"] = 16;

            string sLine = "";
            for (int i = 0; i < lLines.Count; i++)
            {
                sLine = lLines[i];
                if (IsLabelLine(sLine))
                {                                 
                    if ((sLine.Substring(1,sLine.Length-2)[0]<='9'&& sLine.Substring(1, sLine.Length - 2)[0] >= '0') || sLine.Contains("-"))
                         throw new FormatException("Label illegal " + i + ": " + lLines[i]);
                                          
                   else if (labels.ContainsKey(sLine.Substring(1, sLine.Length - 2)))                                 
                         throw new FormatException("Double label " + i + ": " + lLines[i]);
                   else
                   {
                        rowindex = i - numoflabel;                    
                        labels[sLine.Substring(1, sLine.Length - 2)] = rowindex;
                        numoflabel++;
                        rowindex = 0;           
                        if (symbols.ContainsKey(sLine.Substring(1, sLine.Length - 2)))
                           symbols[sLine.Substring(1, sLine.Length - 2)] = labels[sLine.Substring(1, sLine.Length - 2)];

                        
                   }
                    //record label in symbol table
                    //do not add the label line to the result
                }
                else if (IsACommand(sLine))
                {
                    string s = sLine.Substring(1, sLine.Length - 1);
                    int temp = 0;
                    if (!sLine.Contains("-")) {
                        if (!Int32.TryParse(s, out temp)) {                         
                            if (labels.ContainsKey(sLine.Substring(1, sLine.Length - 1)))
                                symbols[s] = labels[s];
                            else if (!(symbols.ContainsKey(s)))
                                symbols[s] = i - numoflabel;
                        }         
                    }                                                                                                                      
               }

                 //may contain a variable - if so, record it to the symbol table (if it doesn't exist there yet...)             
                else if (IsCCommand(sLine))
                {
                    //do nothing here
                }
                else
                    throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);
            }
          
        }
        
        //third pass - translate lines into machine code, replacing symbols with numbers
        private List<string> TranslateAssemblyToMachineCode(List<string> lLines)
        {
            string sLine = "";
            List<string> lAfterPass = new List<string>();
            for (int i = 0; i < lLines.Count; i++)
            {
                sLine = lLines[i];
                if (IsACommand(sLine))
                {
                    int t = 0;                                                                                          
                    string s = sLine.Substring(1, sLine.Length - 1);
                    if (Int32.TryParse(s, out t))
                        lAfterPass.Add(ToBinary(t));

                   else if (symbols.ContainsKey(s))                  
                          lAfterPass.Add(ToBinary(symbols[s]));
                    
                    
                    else
                        throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);                   
                    //translate an A command into a sequence of bits
                }
                else if (IsCCommand(sLine))
                {
                    string sDest, sControl, sJmp;
                    GetCommandParts(sLine, out sDest, out sControl, out sJmp);
                    int[] D, C, J;             
                    //translate an C command into a sequence of bits

                    D = m_dest[sDest];
                    C = m_dControl[sControl];
                    J = m_dJmp[sJmp];                   

                    dest = ToString(D);
                    control = ToString(C);
                    jump = ToString(J);

                    Dcode = "111" + control + dest + jump;
                    lAfterPass.Add(Dcode);

                    //take a look at the dictionaries m_dControl, m_dJmp, and where they are initialized (InitCommandDictionaries), to understand how to you them here
                }
                else if (IsLabelLine(sLine))
                {                   
                    string s = sLine.Substring(1, sLine.Length - 2);
                    if (!labels.ContainsKey(s))                
                        throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);
                }
                else
                    throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);
            }
            return lAfterPass;
        }

        //helper functions for translating numbers or bits into strings
        private string ToString(int[] aBits)
        {
            string sBinary = "";
            for (int i = 0; i < aBits.Length; i++)
                sBinary += aBits[i];
            return sBinary;
        }

        private string ToBinary(int x)
        {
            string sBinary = "";
            for (int i = 0; i < WORD_SIZE; i++)
            {
                sBinary = (x % 2) + sBinary;
                x = x / 2;
            }
            return sBinary;
        }


        //helper function for splitting the various fields of a C command
        private void GetCommandParts(string sLine, out string sDest, out string sControl, out string sJmp)
        {
            if (sLine.Contains('='))
            {
                int idx = sLine.IndexOf('=');
                sDest = sLine.Substring(0, idx);
                sLine = sLine.Substring(idx + 1);
            }
            else
                sDest = "";
            if (sLine.Contains(';'))
            {
                int idx = sLine.IndexOf(';');
                sControl = sLine.Substring(0, idx);
                sJmp = sLine.Substring(idx + 1);

            }
            else
            {
                sControl = sLine;
                sJmp = "";
            }
        }

        private bool IsCCommand(string sLine)
        {
            return !IsLabelLine(sLine) && sLine[0] != '@';
        }

        private bool IsACommand(string sLine)
        {
            return sLine[0] == '@';
        }

        private bool IsLabelLine(string sLine)
        {
            if (sLine.StartsWith("(") && sLine.EndsWith(")"))
                return true;
            return false;
        }

        private string CleanWhiteSpacesAndComments(string sDirty)
        {
            string sClean = "";
            for (int i = 0 ; i < sDirty.Length ; i++)
            {
                char c = sDirty[i];
                if (c == '/' && i < sDirty.Length - 1 && sDirty[i + 1] == '/') // this is a comment
                    return sClean;
                if (c > ' ' && c <= '~')//ignore white spaces
                    sClean += c;
            }
            return sClean;
        }


        private void InitCommandDictionaries()
        {
            m_dControl = new Dictionary<string, int[]>();

            m_dControl["0"] = new int[] { 0, 1, 0, 1, 0, 1, 0 };
            m_dControl["1"] = new int[] { 0, 1, 1, 1, 1, 1, 1 };
            m_dControl["-1"] = new int[] { 0, 1, 1, 1, 0, 1, 0 };
            m_dControl["D"] = new int[] { 0, 0, 0, 1, 1, 0, 0 };
            m_dControl["A"] = new int[] { 0, 1, 1, 0, 0, 0, 0 };
            m_dControl["!D"] = new int[] { 0, 0, 0, 1, 1, 0, 1 };
            m_dControl["!A"] = new int[] { 0, 1, 1, 0, 0, 0, 1 };
            m_dControl["-D"] = new int[] { 0, 0, 0, 1, 1, 1, 1 };
            m_dControl["-A"] = new int[] { 0, 1, 1, 0, 0,1, 1 };
            m_dControl["D+1"] = new int[] { 0, 0, 1, 1, 1, 1, 1 };
            m_dControl["A+1"] = new int[] { 0, 1, 1, 0, 1, 1, 1 };
            m_dControl["D-1"] = new int[] { 0, 0, 0, 1, 1, 1, 0 };
            m_dControl["A-1"] = new int[] { 0, 1, 1, 0, 0, 1, 0 };
            m_dControl["D+A"] = new int[] { 0, 0, 0, 0, 0, 1, 0 };
            m_dControl["D-A"] = new int[] { 0, 0, 1, 0, 0, 1, 1 };
            m_dControl["A-D"] = new int[] { 0, 0, 0, 0, 1,1, 1 };
            m_dControl["D&A"] = new int[] { 0, 0, 0, 0, 0, 0, 0 };
            m_dControl["D|A"] = new int[] { 0, 0, 1, 0,1, 0, 1 };

            m_dControl["M"] = new int[] { 1, 1, 1, 0, 0, 0, 0 };
            m_dControl["!M"] = new int[] { 1, 1, 1, 0, 0, 0, 1 };
            m_dControl["-M"] = new int[] { 1, 1, 1, 0, 0, 1, 1 };
            m_dControl["M+1"] = new int[] { 1, 1, 1, 0, 1, 1, 1 };
            m_dControl["M-1"] = new int[] { 1, 1, 1, 0, 0, 1, 0 };
            m_dControl["D+M"] = new int[] { 1, 0, 0, 0, 0, 1, 0 };
            m_dControl["D-M"] = new int[] { 1, 0, 1, 0, 0, 1, 1 };
            m_dControl["M-D"] = new int[] { 1, 0, 0, 0, 1, 1, 1 };
            m_dControl["D&M"] = new int[] { 1, 0, 0, 0, 0, 0, 0 };
            m_dControl["D|M"] = new int[] { 1, 0, 1, 0, 1, 0, 1 };


            m_dJmp = new Dictionary<string, int[]>();

            m_dJmp[""] = new int[] { 0, 0, 0 };
            m_dJmp["JGT"] = new int[] { 0, 0, 1 };
            m_dJmp["JEQ"] = new int[] { 0, 1, 0 };
            m_dJmp["JGE"] = new int[] { 0, 1, 1 };
            m_dJmp["JLT"] = new int[] { 1, 0, 0 };
            m_dJmp["JNE"] = new int[] { 1, 0, 1 };
            m_dJmp["JLE"] = new int[] { 1, 1, 0 };
            m_dJmp["JMP"] = new int[] { 1, 1, 1 };

            m_dest = new Dictionary<string, int[]>();
            m_dest[""] = new int[] {0, 0, 0};
            m_dest["M"] = new int[] { 0, 0, 1 };
            m_dest["D"] = new int[] { 0, 1, 0 };
            m_dest["MD"] = new int[] { 0, 1, 1 };
            m_dest["A"] = new int[] { 1, 0, 0 };
            m_dest["AM"] = new int[] { 1, 0, 1 };
            m_dest["AD"] = new int[] { 1, 1, 0 };
            m_dest["AMD"] = new int[] { 1, 1, 1 };



        }
    }
}

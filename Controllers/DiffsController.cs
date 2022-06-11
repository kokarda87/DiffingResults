using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiffingResults.Controllers
{
    

    //[Route("api")]
    public class DiffsController : ControllerBase
    {
        //I have used Dictionary for temporary data storage 
        private readonly ConcurrentDictionary<string, string> myStorage;
        
        public DiffsController(ConcurrentDictionary<string, string> myStorage)
        {
            this.myStorage = myStorage;
        }


        
        /// <summary>
        ///  API that receives data and the data is an serialized object ReceivedData
        /// </summary>
        /// <param name="id"></param>
        /// <param name="receivedData"></param>
        /// <returns></returns>
        [HttpPut("/v1/diff/{id}/left")]
        public IActionResult PutValueLeft(int id, [FromBody]ReceivedData receivedData)
        {
            if(receivedData.Data == null)
                return BadRequest();

            string myKey = $"{id}left";
            myStorage.AddOrUpdate(myKey, receivedData.Data, (key, oldValue) => receivedData.Data);
            return StatusCode(StatusCodes.Status201Created);
        }

        /// <summary>
        /// API that receives data and the data is an serialized object ReceivedData
        /// </summary>
        /// <param name="id"></param>
        /// <param name="receivedData"></param>
        /// <returns></returns>
        [HttpPut("/v1/diff/{id}/right")]
        public IActionResult PutValueRight(int id, [FromBody]ReceivedData receivedData)
        {
            if (receivedData.Data == null)
                return BadRequest();

            string myKey = $"{id}right";
            myStorage.AddOrUpdate(myKey, receivedData.Data, (key, oldValue) => receivedData.Data);
            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpGet("/v1/diff/{id}")]
        public IActionResult GetValue(int id, string ime)
        {
            // if we didn't find left element with requested ID then we return NotFound status
            if (!myStorage.TryGetValue($"{id}left", out string myLeftValue))
                return BadRequest();
            // if we didn't find left element with requested ID then we return NotFound status
            if (!myStorage.TryGetValue($"{id}right", out string myRightValue))
                return BadRequest();

            if (myLeftValue == myRightValue)
                return Ok(new DifferencesTotal { DiffResultType = "Equals" });

            //decoding from byte64 value and a try-catch that informs us if the values are not in a correct format
            byte[] leftBinary;
            byte[] rightBinary;
            try
            {
                leftBinary = Convert.FromBase64String(myLeftValue);
                rightBinary = Convert.FromBase64String(myRightValue);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            // If lenghts of decoded values are different then we return response where we will tell the caller that values are not the same size.
            if (leftBinary.Length != rightBinary.Length)
                return Ok(new DifferencesTotal { DiffResultType = "SizeDoNotMatch" });

            // If we came to this point then we are certian that values are the same size but are not of similar characters
            return Ok(new DifferencesTotal { DiffResultType = "ContentDoNotMatch", Diffs = GetDifferences(leftBinary, rightBinary) });
        }
        List<Diff> GetDifferences(byte[] left, byte[] right)
        {
            // We will store first index where values don't align. 
            int startIndex = -1;
            List<Diff> differences = new List<Diff>();

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    // Found difference. If this is difference the first difference then we memorize the index.
                    if (startIndex == -1)
                        startIndex = i;
                }
                else
                    if(startIndex !=-1)
                {
                    differences.Add(new Diff { offset = startIndex, length = i - startIndex });
                    startIndex = -1; // we set this difference index to default value so we know that we didn't find any differences from the last one.
                }
            }
            // We finished the loop and now we just need to check if difference index is different than default value (-1). If it's different then this means that there are differences from that index until the end
            if (startIndex != -1)
                differences.Add(new Diff { offset = startIndex, length = left.Length - startIndex });

            return differences;
        }
    }

    public class ReceivedData
    {
        public string Data { get; set; }
    }

    public class Diff
    {
        public int offset { get; set; }

        public int length { get; set; }
    }

    public class DifferencesTotal
    {
        public string DiffResultType { get; set; }
        public List<Diff> Diffs { get; set; }
    }
     
}
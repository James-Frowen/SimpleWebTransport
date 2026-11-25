Uses [CopyBufferBenchmark](./CopyBufferBenchmark.cs) to test the speed of different copy methods for byte[]

*Smaller array count has more iteration*

|  Method      |  Array Size  |  ElapsedMilliseconds |
|--------------|--------------|----------------------|
|  forInline   |  10          |   691                |
|  forMethod   |  10          |   721                |
|  ArrayCopy   |  10          |   1529               |
|  BufferCopy  |  10          |   1117               |
|              |              |                      |
|  forInline   |  20          |   589                |
|  forMethod   |  20          |   628                |
|  ArrayCopy   |  20          |   710                |
|  BufferCopy  |  20          |   534                |
|              |              |                      |
|  forInline   |  100         |   527                |
|  ArrayCopy   |  100         |   169                |
|  BufferCopy  |  100         |   124                |
|              |              |                      |
|  forInline   |  800         |   499                |
|  ArrayCopy   |  800         |   54                 |
|  BufferCopy  |  800         |   28                 |
|              |              |                      |
|  forInline   |  2000        |   470                |
|  ArrayCopy   |  2000        |   39                 |
|  BufferCopy  |  2000        |   14                 |
|              |              |                      |
|  forInline   |  16000       |   463                |
|  ArrayCopy   |  16000       |   32                 |
|  BufferCopy  |  16000       |   9                  |
|              |              |                      |
|  forInline   |  65535       |   463                |
|  ArrayCopy   |  65535       |   33                 |
|  BufferCopy  |  65535       |   19                 |

**Summary**

- for loop (method or inline) for small size ~10
- Buffer.BlockCopy for other 20+




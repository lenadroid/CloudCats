#load "credentials.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open System.Threading
open System
open FSharp.Data
open FSharp.Charting
open System.Net
open System.Drawing

let runtime = Runtime.GetHandle(config)
runtime.ShowWorkers()
runtime.ShowProcesses()
runtime.AttachClientLogger(ConsoleLogger())

let catA59281Image = """https://gs1.wac.edgecastcdn.net/8019B6/data.tumblr.com/tumblr_m27d9lToVP1qze0hyo1_1280.jpg"""
let cat3Image = """https://harthur.github.io/kittydar/images/cat3.jpg"""
let catApiUrl = "http://thecatapi.com/api/images/get?type=png"
let catServer = "cat server url"

type CatCoords =
    | NoCat
    | Cat of Rectangle

let loadImage (url : string) = cloud {
    let req = HttpWebRequest.Create(url)
    let! response = req.AsyncGetResponse() |> Cloud.OfAsync 
    return new Bitmap(response.GetResponseStream())
} 

let getDominantColor (bmp : Bitmap) = cloud {
    let r = ref 0
    let g = ref 0
    let b = ref 0
    let total = ref 0
    for x in 0 .. bmp.Width - 1 do
        for y in 0..bmp.Height - 1 do
            let clr = bmp.GetPixel(x, y)
            r := int(clr.R) + !r
            g := int(clr.G) + !g
            b := int(clr.B) + !b
            total := !total + 1
    r := !r / !total
    g := !g / !total
    b := !b / !total
    return Color.FromArgb(!r,!g,!b)
}

let colorToHex (c : Color) = cloud {
    return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2")
}

let cropImage (img : Bitmap) (cropArea : Rectangle) = cloud {
   let bmpCrop = img.Clone(cropArea, img.PixelFormat)
   return bmpCrop
}

let getCatFaceCoords (url : string) = cloud {
    let http = new System.Net.WebClient()
    let! json = http.AsyncDownloadString(Uri url) |> Cloud.OfAsync
    return json
}

let JsonToCatCoords (json : string) : Cloud<CatCoords> = cloud {
    try
        let jsonObject = JsonValue.Parse json
        let arr = jsonObject.GetProperty("result").AsArray()
        let result = arr |> Array.map (fun i -> 
                                        try
                                            let xx = i.GetProperty("x")
                                            let yy = i.GetProperty("y")
                                            let heightt = i.GetProperty("height")
                                            let widthh = i.GetProperty("width")
                                            Cat(Rectangle(xx.AsInteger(),yy.AsInteger(),widthh.AsInteger(), heightt.AsInteger()))
                                        with
                                            | ex -> NoCat
                                        )

        return Array.get result 0
    with
    | ex -> return NoCat
}

let catCoordsFromUrl (url : string) = cloud {
    let! jsonCatCoords = getCatFaceCoords (catServer+url)
    let! coords = JsonToCatCoords jsonCatCoords
    match coords with 
    | NoCat -> return None
    | Cat cat -> return Some(cat)
}

let translateColor hexColor = ColorTranslator.FromHtml(hexColor)

let colorsDiff (c1: Color) (c2: Color) = 
    let rDiff = float ((int c1.R - int c2.R) |> abs) / 255.0
    let gDiff = float ((int c1.G - int c2.G) |> abs) / 255.0
    let bDiff = float ((int c1.B - int c2.B) |> abs) / 255.0
    (rDiff + gDiff + bDiff) / 3.0 * 100.0

let getRandomCatUrl = 
     let request = catApiUrl |> WebRequest.Create :?> HttpWebRequest
     request.AllowAutoRedirect <- false
     let response =  request.GetResponse() :?> HttpWebResponse
     let redirUrl = response.Headers.Get("Location")
     redirUrl


let getCatColor (desired : string) (percent : float) (url : string) = cloud {
    let! jsonCatCoords = getCatFaceCoords (catServer+url)
    let! catCoords = JsonToCatCoords jsonCatCoords
    match catCoords with
    | NoCat -> return None
    | Cat cat -> 
        let! bmp = url |> loadImage
        let! croppedBmp = cropImage bmp cat
        let! dominantColor = croppedBmp |> getDominantColor 
        let! hexcolor = dominantColor |> colorToHex
        if ((colorsDiff (translateColor hexcolor) (translateColor desired)) > percent) 
        then return None
        else return Some(hexcolor)
} 

[<Cloud>]
let getFirstCatColor (getCatColor : string -> Cloud<string option>) (inputs : seq<string>) = cloud {
    let! result = Cloud.Choice (Seq.map getCatColor inputs)
    match result with
    | Some s -> return result.Value
    | None -> return "No cat found"
}

let queries = Array.init 100 (fun x -> getRandomCatUrl) // [cat3Image;catA59281Image]

let catProcess = runtime.CreateProcess (getFirstCatColor (getCatColor "#FFFFFF" 50.0) queries)
let catResults = catProcess.AwaitResult()
printfn "%A" catResults



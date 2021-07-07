module dash_stock_ticker_less_loc

open Dash.NET; open Plotly.NET; open FSharp.Data; open Deedle; open System; open System.IO; open System.Text; open System.Text.RegularExpressions

let req = Http.Request("https://finance.yahoo.com/quote/AMZN/history?p=AMZN",httpMethod=HttpMethod.Get)
let body = match req.Body with | HttpResponseBody.Text b -> b
let crumb = Regex("CrumbStore\":{\"crumb\":\"(?<crumb>.+?)\"}").Match(body).Groups.["crumb"].Value
let cookie = req.Cookies.["B"]

let getDf ticker : Frame<System.DateTime,string> = 
    let response =
        Http.RequestString(        
            $"https://query1.finance.yahoo.com/v7/finance/download/{ticker}",
            query= ["period1","1167609600"; "period2",(string System.DateTime.Now.Ticks); "crumb",crumb],
            cookies=["B",cookie]
        )
    use stream = new MemoryStream(Encoding.UTF8.GetBytes(response))
    Frame.ReadCsv(stream,true,separators=",") |> Frame.indexRows "Date"

open Dash.NET.Html; open Dash.NET.DCC; open ComponentPropTypes

let layout = 
    Html.div [
        Attr.children [
            Dropdown.dropdown "stock-dropdown" [
                Dropdown.Options [
                    DropdownOption.create "Coke" "COKE" false "Coke"
                    DropdownOption.create "Tesla" "TSLA" false "Tesla"
                    DropdownOption.create "Apple" "AAPL" false "Apple"
                ]
                Dropdown.Value (Dropdown.DropdownValue.SingleValue "COKE")
            ] []
            Graph.graph "stock-graph" [] []
        ]
    ]

open Dash.NET.Operators

let callback =
    Callback.singleOut(
        "stock-dropdown" @. Value,
        "stock-graph" @. (CustomProperty "figure"),
        (fun (ticker:string) -> 
            "stock-graph" @. (CustomProperty "figure") => (Chart.Line((getDf ticker).["Close"] |> Series.observations)|> GenericChart.toFigure)
        ),
        PreventInitialCall = false
    )

let myDashApp =
    DashApp.initDefault()
    |> DashApp.withLayout layout
    |> DashApp.addCallback callback
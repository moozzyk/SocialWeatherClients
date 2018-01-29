ws = websocket.createClient()

ws:on("connection", function(ws)
  print('Connected to social weather')
end)

ws:on("receive", function(_, msg, opcode)
  print('Weather updated');

  captions = { "Temperature: ", "Last updated: ", "Weather: ", "ZipCode: " }
  weather = { "Sunny", "Mostly Sunny", "Partly Sunny", "Partly Cloudy", "Mostly Cloudy", "Cloudy" }

  local i = 1
  for token in string.gmatch(msg, "([^|]+)") do
    if (i == 3) then
      print(captions[i], weather[tonumber(token) + 1])
    else
      print(captions[i], token)
    end
    i = i + 1
  end

end)

ws:on("close", function(_, status)
  print('connection closed', status)
  ws = nil -- required to lua gc the websocket client
end)

-- update the url to point to your social weather server
ws:connect('ws://socialweather.azurewebsites.net/weather?formatType=pipe')

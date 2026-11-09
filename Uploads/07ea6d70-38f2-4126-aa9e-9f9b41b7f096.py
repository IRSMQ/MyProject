import socket

s = socket.socket()

PORT = 4424

s.bind(("0.0.0.0",PORT))
s.listen(5)

client_socket, address = s.accept()

s.send("https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-10.0&tabs=visual-studio-code".encode())
print("Close")
s.close()
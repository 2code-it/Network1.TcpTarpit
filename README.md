# Network1.TcpTarpit
Tarpit for tcp connections

# Program help/arguments
This program is used to host tcp tarpitting endpoints.
example: Network1.TcpTarpitApp.exe -a 192.168.12.21 -p 2000-3000,5001 -d 400 -b 3 -l ./logs -r ./bogus.txt -m 600
options: 
	-a Listen address, ipaddress format e.g. 192.168.12.21
	-p Ports, a comma separated string of ports and/or port ranges e.g. 2000,3000,4000-4020,5000
	-d (optional) Iteration delay in ms, default: 200
	-b (optional) Bytes per iteration, default: 1
	-l (optional) Connection log directory, path to directory to store connection logs, default: null (nolog)
	-r (optional) Response data file, path to a file containing response data, default: null (random bytes)
	-m (optional) Max connection time in seconds, default: 0 (infinite)
    -h Display (this) help

# Example
Network1.TcpTarpitApp.exe -a 192.168.12.21 -p 2001-3000,5001 -d 400 -b 3 -l ./logs -r ./bogus.txt -m 600

When started as above:
-The program starts 1001 TcpListeners on ports 2001 till 3000 and 5001 bound to address 192.168.12.21
-Response bytes are sourced from the specified response data file ./bogus.txt (of not specified than random bytes)
-When the logs directory is specfied, it will check en ensure existance
-Each listener accepts any incoming tcp connection and starts an iteration to send some data
-Each iteration sends 3 bytes and is delayed by 400ms
-When the connection uses more bytes than available it will start from the beginning
-When the connection time exceeds over 600 seconds it is closed
-When the connection is closed a line is written to the daily log file in the logs directory

# Log file text format (tab delimited)
-timestamp HH:mm:ss
-remoteEndpoint ip:port
-localEndpoint ip:port
-durationInSeconds seconds
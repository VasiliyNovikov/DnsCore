from typing import List
from dnslib import RCODE, DNSRecord
from dnslib.server import DNSServer, BaseResolver, DNSHandler


class TestResolver(BaseResolver):
    def __init__(self, responses: List[DNSRecord]) -> None:
        self.__responses = responses
        self.__response_index = -1

    def resolve(self, request: DNSRecord, handler: DNSHandler) -> DNSRecord:
        try:
            self.__response_index += 1
            return self.__responses[self.__response_index]
        except Exception as e:
            print(e)
            reply = super().resolve(request, handler)
            reply.header.rcode = RCODE.SERVFAIL
            return reply


class TestDNSServer:
    def __init__(self, port: int, responses: List[bytes]) -> None:
        self.__port = port
        self.__responses = [DNSRecord.parse(response) for response in responses]
        self.__udp_server = None
        self.__tcp_server = None

    def start(self) -> None:
        resolver = TestResolver(self.__responses)
        self.__udp_server = DNSServer(resolver, port=self.__port, tcp=False)
        self.__tcp_server = DNSServer(resolver, port=self.__port, tcp=True)
        self.__udp_server.start_thread()
        self.__tcp_server.start_thread()

    def stop(self) -> None:
        self.__udp_server.stop()
        self.__tcp_server.stop()
        self.__udp_server = None
        self.__tcp_server = None


def start(port: int, responses: List[bytes]) -> TestDNSServer:
    server = TestDNSServer(port, responses)
    server.start()
    return server


def stop(server: TestDNSServer) -> None:
    server.stop()

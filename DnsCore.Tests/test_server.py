import json
from typing import TypedDict, List, Dict, Type, Optional
import sys
import time

from dnslib import RR, RCODE, DNSRecord, RD, A, AAAA, QTYPE, CNAME
from dnslib.server import DNSServer, BaseResolver, DNSHandler


class Question(TypedDict):
    name: str
    type: str


class Record(Question):
    ttl: int
    data: str


class Message(TypedDict):
    question: Question
    answers: List[Record]


RDATA: Dict[str, Type[RD]] = {
    'A': A,
    'AAAA': AAAA,
    'CNAME': CNAME
}


DEFAULT_MESSAGES: List[Message] = [
    {
        'question': {
            'name': 'example.com.',
            'type': 'A'
        },
        'answers': [
            {
                'name': 'example.com.',
                'type': 'A',
                'ttl': 60,
                'data': '1.2.3.4'
            }
        ]
    },
    {
        'question': {
            'name': 'example.com.',
            'type': 'AAAA'
        },
        'answers': [
            {
                'name': 'example.com.',
                'type': 'A',
                'ttl': 60,
                'data': '::1:2:3:4'
            }
        ]
    }
]


class TestResolver(BaseResolver):
    def __init__(self, messages: List[Message] = None) -> None:
        self.__messages = messages or DEFAULT_MESSAGES

    def resolve(self, request: DNSRecord, handler: DNSHandler) -> DNSRecord:
        try:
            reply = super().resolve(request, handler)
            question = request.q
            qname = question.qname
            qtype = QTYPE[question.qtype]
            for message in self.__messages:
                question = message['question']
                if question['name'] == qname and question['type'] == qtype:
                    for record in message['answers']:
                        rtype = record['type']
                        reply.add_answer(RR(record['name'], QTYPE.reverse[rtype], ttl=record['ttl'], rdata=RDATA[rtype](record['data'])))
                    reply.header.rcode = RCODE.NOERROR
                    break
            return reply
        except Exception as e:
            print(e)
            reply = super().resolve(request, handler)
            reply.header.rcode = RCODE.SERVFAIL
            return reply


def main(port: int, message_file: Optional[str] = None) -> None:
    if message_file:
        with open(message_file) as f:
            messgaes_str = f.read()
            print(messgaes_str)
            messages = json.loads(messgaes_str)
    else:
        messages = None

    resolver = TestResolver(messages)

    udp_server = DNSServer(resolver, port=port, tcp=False)
    tcp_server = DNSServer(resolver, port=port, tcp=True)

    udp_server.start_thread()
    tcp_server.start_thread()

    try:
        while True:
            time.sleep(0.1)
    except KeyboardInterrupt:
        pass
    finally:
        udp_server.stop()
        tcp_server.stop()


if __name__ == '__main__':
    port = int(sys.argv[1])
    message_file = sys.argv[2] if len(sys.argv) > 2 else None
    main(port, message_file)

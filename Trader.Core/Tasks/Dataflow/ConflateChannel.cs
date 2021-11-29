using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Outcompute.Trader.Core.Tasks.Dataflow
{
    public class ConflateChannel<TInput, TConflate, TOutput> : Channel<TInput, TOutput>
    {
        private readonly Func<TConflate> _open;
        private readonly Func<TConflate, TInput, TConflate> _conflate;
        private readonly Func<TConflate, TOutput> _close;

        public ConflateChannel(Func<TConflate> open, Func<TConflate, TInput, TConflate> conflate, Func<TConflate, TOutput> close)
        {
            Guard.IsNotNull(open, nameof(open));
            Guard.IsNotNull(conflate, nameof(conflate));
            Guard.IsNotNull(close, nameof(close));

            _open = open;
            _conflate = conflate;
            _close = close;

            Writer = new ConflateChannelWriter(this);
            Reader = new ConflateChannelReader(this);
        }

        private readonly Channel<TConflate> _conflations = Channel.CreateUnbounded<TConflate>();

        private sealed class ConflateChannelWriter : ChannelWriter<TInput>
        {
            private readonly ConflateChannel<TInput, TConflate, TOutput> _channel;

            public ConflateChannelWriter(ConflateChannel<TInput, TConflate, TOutput> channel)
            {
                _channel = channel;
            }

            public override bool TryWrite(TInput item)
            {
                TConflate conflation;
                if (!_channel._conflations.Reader.TryRead(out conflation!))
                {
                    conflation = _channel._open();
                }

                conflation = _channel._conflate(conflation, item);

                return _channel._conflations.Writer.TryWrite(conflation);
            }

            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
            {
                return _channel._conflations.Writer.WaitToWriteAsync(cancellationToken);
            }
        }

        private sealed class ConflateChannelReader : ChannelReader<TOutput>
        {
            private readonly ConflateChannel<TInput, TConflate, TOutput> _channel;

            public ConflateChannelReader(ConflateChannel<TInput, TConflate, TOutput> channel)
            {
                _channel = channel;
            }

            public override bool TryRead([MaybeNullWhen(false)] out TOutput item)
            {
                if (_channel._conflations.Reader.TryRead(out var conflation))
                {
                    item = _channel._close(conflation);
                    return true;
                }

                item = default;
                return false;
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
            {
                return _channel._conflations.Reader.WaitToReadAsync(cancellationToken);
            }
        }
    }
}
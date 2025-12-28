#!/usr/bin/env python3
"""
Naglfar Analytics End-to-End Testing CLI

This CLI tool simulates different user journeys through the Naglfar Analytics system,
including authentication, browsing books, and making purchases.
"""

import argparse
import sys
from typing import Optional
from journeys.browse_books import BrowseBooksJourney
from journeys.purchase_book import PurchaseBookJourney
from journeys.full_user_flow import FullUserFlowJourney
from config import Config


def create_parser() -> argparse.ArgumentParser:
    """Create and configure the argument parser."""
    parser = argparse.ArgumentParser(
        description="Naglfar Analytics End-to-End Testing Tool",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Browse books from store-1
  %(prog)s browse --store-id store-1

  # Purchase a book
  %(prog)s purchase --store-id store-1 --book-id 1

  # Run full user flow (browse + purchase)
  %(prog)s full-flow --store-id store-1

  # Use custom base URL
  %(prog)s browse --base-url http://api.local --store-id store-2

  # Verbose output
  %(prog)s browse --store-id store-1 --verbose
        """
    )

    # Global arguments
    parser.add_argument(
        '--base-url',
        default='http://localhost',
        help='Base URL for the API (default: http://localhost)'
    )
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help='Enable verbose output'
    )

    # Subcommands
    subparsers = parser.add_subparsers(dest='command', help='Available commands')

    # Browse books command
    browse_parser = subparsers.add_parser(
        'browse',
        help='Browse books from a store'
    )
    browse_parser.add_argument(
        '--store-id',
        required=True,
        help='Store ID (e.g., store-1, store-2, ...)'
    )

    # Purchase book command
    purchase_parser = subparsers.add_parser(
        'purchase',
        help='Purchase a book from a store'
    )
    purchase_parser.add_argument(
        '--store-id',
        required=True,
        help='Store ID (e.g., store-1, store-2, ...)'
    )
    purchase_parser.add_argument(
        '--book-id',
        type=int,
        required=True,
        help='Book ID to purchase'
    )
    purchase_parser.add_argument(
        '--quantity',
        type=int,
        default=1,
        help='Quantity to purchase (default: 1)'
    )

    # Full user flow command
    full_flow_parser = subparsers.add_parser(
        'full-flow',
        help='Run complete user journey (browse + add to cart + checkout)'
    )
    full_flow_parser.add_argument(
        '--store-id',
        required=True,
        help='Store ID (e.g., store-1, store-2, ...)'
    )
    full_flow_parser.add_argument(
        '--num-books',
        type=int,
        default=1,
        help='Number of different books to purchase (default: 1)'
    )

    return parser


def main() -> int:
    """Main entry point for the CLI."""
    parser = create_parser()
    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        return 1

    # Initialize configuration
    config = Config(
        base_url=args.base_url,
        verbose=args.verbose
    )

    try:
        # Execute the appropriate command
        if args.command == 'browse':
            journey = BrowseBooksJourney(config)
            result = journey.run(args.store_id)

        elif args.command == 'purchase':
            journey = PurchaseBookJourney(config)
            result = journey.run(
                args.store_id,
                args.book_id,
                args.quantity
            )

        elif args.command == 'full-flow':
            journey = FullUserFlowJourney(config)
            result = journey.run(
                args.store_id,
                args.num_books
            )
        else:
            print(f"Unknown command: {args.command}", file=sys.stderr)
            return 1

        # Print results
        if config.verbose:
            print("\n=== Test Results ===")
            print(f"Status: {'✅ PASSED' if result.success else '❌ FAILED'}")
            print(f"Duration: {result.duration:.2f}s")
            if result.error:
                print(f"Error: {result.error}")

        return 0 if result.success else 1

    except KeyboardInterrupt:
        print("\n\nTest interrupted by user", file=sys.stderr)
        return 130
    except Exception as e:
        print(f"\n❌ Error: {e}", file=sys.stderr)
        if config.verbose:
            import traceback
            traceback.print_exc()
        return 1


if __name__ == '__main__':
    sys.exit(main())

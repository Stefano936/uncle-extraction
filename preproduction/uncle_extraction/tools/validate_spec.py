"""Validate the preproduction catalogs, not the future game's behavior.

Run from any directory: python tools/validate_spec.py
No external Python dependency is required. Does not modify files.
"""
from pathlib import Path
import csv
import json

ROOT = Path(__file__).resolve().parents[1]


def load_csv(name):
    with (ROOT / 'data' / name).open(encoding='utf-8-sig', newline='') as stream:
        rows = list(csv.DictReader(stream))
    assert rows and all(None not in row for row in rows), name
    if 'id' in rows[0]:
        assert len({row['id'] for row in rows}) == len(rows), name + ': duplicate IDs'
    return rows


def main():
    b = json.loads((ROOT / 'data/game_balance.json').read_text(encoding='utf-8'))
    assert b['status'] == 'PREPRODUCTION_UNTESTED'
    assert [row['players'] for row in b['party_scaling']] == [1, 2, 3, 4]
    assert len(b['difficulties']) == 3 and len(b['attacks']) == 4
    assert b['player']['grab_reach_m'] < b['player']['grab_break_error_m']
    assert b['player']['grab_warn_error_m'] < b['player']['grab_break_error_m']
    for attack in b['attacks']:
        assert min(attack['windup_s'], attack['active_s'], attack['recovery_s']) > 0
        for difficulty in b['difficulties']:
            windup = max(b['attack_rules']['minimum_telegraph_s'],
                         attack['windup_s'] * difficulty['telegraph_multiplier'])
            assert windup >= 0.8
    tasks = load_csv('backlog.csv')
    pending = {r['id']: set(r['dependencies'].split('|')) - {''} for r in tasks}
    ids = set(pending)
    assert all(deps <= ids for deps in pending.values()), 'Unknown task dependency'
    done = set()
    while pending:
        ready = {key for key, deps in pending.items() if deps <= done}
        assert ready, 'Dependency cycle'
        done.update(ready)
        for key in ready:
            del pending[key]
    states = load_csv('npc_states.csv')
    valid_states = {row['state'] for row in states}
    assert len(valid_states) == len(states)
    assert all(set(row['next_states'].split('|')) <= valid_states for row in states)
    dialogs = load_csv('dialogue_en_es.csv')
    assert all(row['text_en'] and row['text_es'] and row['clean_en'] and row['clean_es']
               for row in dialogs)
    assets = load_csv('asset_manifest.csv')
    for asset in assets:
        if asset['status'] == 'delivered':
            assert (ROOT / asset['path']).is_file(), asset['path']
    qa = load_csv('qa_matrix.csv')
    print('Catalog validation OK. This is not a gameplay test.')
    print(f'{len(states)} states; {len(dialogs)} dialogue lines; {len(assets)} assets; '
          f'{len(tasks)} tasks; {len(qa)} QA cases.')
    print('Derived initial configurations:')
    for party in b['party_scaling']:
        for difficulty in b['difficulties']:
            resistance = party['resistance_factor'] * difficulty['resistance_multiplier']
            interval = party['attack_interval_s'] * difficulty['attack_interval_multiplier']
            timer = (difficulty['base_round_time_s'] + party['time_assist_s']
                     if difficulty['timer_enabled'] else None)
            print(f'N={party["players"]} {difficulty["id"]}: R={resistance:.3f}; '
                  f'attack interval >= {interval:.2f}s; timer={timer}')


if __name__ == '__main__':
    main()
